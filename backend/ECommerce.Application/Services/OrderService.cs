using ECommerce.Domain;

namespace ECommerce.Application;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartService _cartService;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        ICartService cartService,
        IProductVariantRepository variantRepository,
        IAddressRepository addressRepository,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _cartService = cartService;
        _variantRepository = variantRepository;
        _addressRepository = addressRepository;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request, string clientIp, string returnUrl)
    {
        if (request.PaymentMethod != "COD" && request.PaymentMethod != "VNPay")
            throw new OrderException(400, "Phương thức thanh toán không hợp lệ.");

        // Toàn bộ luồng (kiểm tra & trừ tồn kho -> tạo Order -> xoá giỏ hàng -> tạo Payment)
        // được bọc trong 1 transaction để đảm bảo tính nguyên tử (atomic) theo đúng thiết kế TH_P401.
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var cart = await _cartService.GetCartAsync(userId);
            if (cart.Items.Count == 0)
                throw new OrderException(400, "Giỏ hàng của bạn đang trống.");

            var address = await _addressRepository.GetByIdAsync(request.AddressId);
            if (address is null || address.UserId != userId)
                throw new OrderException(400, "Địa chỉ giao hàng không hợp lệ.");

            // RACE-CONDITION FIX (BR-02): trừ kho bằng UPDATE nguyên tử có điều kiện
            // (StockQuantity >= quantity), thay cho pattern cũ "đọc số lượng -> kiểm tra
            // ở C# -> ghi lại qua change tracker" vốn KHÔNG atomic. Với pattern cũ, 2
            // Customer đặt hàng cùng lúc cho cùng 1 variant có thể cùng đọc được số
            // lượng còn đủ hàng (vd: còn 1 cái) rồi cùng được phép trừ kho, dẫn tới bán
            // vượt tồn kho thực tế (vi phạm trực tiếp BR-02). Nếu bất kỳ item nào không
            // đủ hàng tại thời điểm trừ, toàn bộ transaction rollback (kể cả các item đã
            // trừ thành công trước đó trong cùng vòng lặp), nên không cần rollback tay.
            foreach (var item in cart.Items)
            {
                var decremented = await _variantRepository.TryDecrementStockAsync(item.VariantId, item.Quantity);
                if (!decremented)
                    throw new OrderException(400, $"Sản phẩm {item.ProductName} không đủ số lượng tồn kho.");
            }

            var order = new Order
            {
                UserId = userId,
                AddressId = request.AddressId,
                ShippingMethod = request.ShippingMethod,
                TotalAmount = cart.TotalAmount,
                Status = "Pending",
                CancelReason = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    ProductVariantId = i.VariantId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price
                }).ToList(),
                OrderStatusLogs = new List<OrderStatusLog>
                {
                    new OrderStatusLog
                    {
                        Status = "Pending",
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = userId
                    }
                }
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // Xoá giỏ hàng
            await _cartService.ClearCartAsync(userId);

            // Khởi tạo thanh toán ngay khi tạo đơn (COD -> tự xác nhận đơn; VNPay -> sinh paymentUrl)
            var payment = await _paymentService.CreatePaymentAsync(
                userId,
                new CreatePaymentRequest(order.Id, request.PaymentMethod),
                clientIp,
                returnUrl);

            await transaction.CommitAsync();

            return MapToResponse(order) with { PaymentUrl = payment.PaymentUrl };
        }
        catch (OrderException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (PaymentException ex)
        {
            await transaction.RollbackAsync();
            throw new OrderException(ex.StatusCode, ex.Message);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw new OrderException(500, "Đặt hàng thất bại, vui lòng thử lại.");
        }
    }

    public async Task<OrderListResponse> GetUserOrdersAsync(int userId, int page, int pageSize)
    {
        var orders = await _orderRepository.GetUserOrdersAsync(userId, page, pageSize);
        var totalCount = await _orderRepository.GetUserOrdersCountAsync(userId);
        return new OrderListResponse(
            orders.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<OrderDetailResponse> GetOrderDetailAsync(int userId, int orderId, bool isAdmin = false)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
            ?? throw new OrderException(404, "Đơn hàng không tồn tại.");

        if (!isAdmin && order.UserId != userId)
            throw new OrderException(403, "Bạn không có quyền xem đơn hàng này.");

        return MapToDetailResponse(order);
    }

    public async Task<OrderResponse> CancelOrderAsync(int userId, int orderId, string? reason)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
            ?? throw new OrderException(404, "Đơn hàng không tồn tại.");

        if (order.UserId != userId)
            throw new OrderException(403, "Bạn không có quyền hủy đơn hàng này.");

        if (order.Status != "Pending")
            throw new OrderException(400, "Chỉ có thể hủy đơn hàng đang chờ xử lý.");

        order.Status = "Cancelled";
        order.CancelReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        // Hoàn kho (BR-10) — dùng UPDATE nguyên tử, đồng bộ với TryDecrementStockAsync
        // ở CreateOrderAsync thay vì đọc-sửa-ghi qua change tracker như trước.
        foreach (var item in order.OrderItems)
        {
            await _variantRepository.IncrementStockAsync(item.ProductVariantId, item.Quantity);
        }

        order.OrderStatusLogs.Add(new OrderStatusLog
        {
            Status = "Cancelled",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = userId
        });

        await _orderRepository.SaveChangesAsync();
        return MapToResponse(order);
    }

    public async Task<OrderListResponse> GetAllOrdersAsync(int page, int pageSize, string? status)
    {
        var orders = await _orderRepository.GetAllOrdersAsync(page, pageSize, status);
        var totalCount = await _orderRepository.GetAllOrdersCountAsync(status);
        return new OrderListResponse(
            orders.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(int adminUserId, int orderId, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
            ?? throw new OrderException(404, "Đơn hàng không tồn tại.");

        var validTransitions = new Dictionary<string, string[]>
        {
            ["Pending"] = new[] { "Confirmed", "Cancelled" },
            ["Confirmed"] = new[] { "Processing", "Cancelled" },
            ["Processing"] = new[] { "Shipping", "Cancelled" },
            ["Shipping"] = new[] { "Delivered", "Cancelled" }
        };

        if (!validTransitions.ContainsKey(order.Status) ||
            !validTransitions[order.Status].Contains(request.Status))
        {
            throw new OrderException(400, $"Không thể chuyển trạng thái từ {order.Status} sang {request.Status}.");
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        order.OrderStatusLogs.Add(new OrderStatusLog
        {
            Status = request.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = adminUserId
        });

        // Hoàn kho nếu Admin hủy đơn — cùng cơ chế atomic với CancelOrderAsync.
        if (request.Status == "Cancelled")
        {
            foreach (var item in order.OrderItems)
            {
                await _variantRepository.IncrementStockAsync(item.ProductVariantId, item.Quantity);
            }
        }

        await _orderRepository.SaveChangesAsync();
        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.OrderItems.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductVariantId,
            i.ProductVariant.ProductId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.VariantName,
            i.Quantity,
            i.UnitPrice,
            i.UnitPrice * i.Quantity
        )).ToList();

        return new OrderResponse(
            order.Id,
            order.Id.ToString().PadLeft(8, '0'),
            order.TotalAmount,
            order.Status,
            order.ShippingMethod,
            order.CancelReason,
            items,
            order.CreatedAt,
            null,
            order.User?.FullName
        );
    }

    private static OrderDetailResponse MapToDetailResponse(Order order)
    {
        var items = order.OrderItems.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductVariantId,
            i.ProductVariant.ProductId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.VariantName,
            i.Quantity,
            i.UnitPrice,
            i.UnitPrice * i.Quantity,
            i.Review != null
        )).ToList();

        var address = new AddressResponse(
            order.Address.Id,
            order.Address.RecipientName,
            order.Address.Phone,
            order.Address.DetailAddress,
            order.Address.Ward,
            order.Address.District,
            order.Address.Province
        );

        var payment = order.Payment is not null ? new PaymentResponse(
            order.Payment.Id,
            order.Payment.Method,
            order.TotalAmount,
            order.Payment.Status,
            order.Payment.TransactionCode,
            order.Payment.PaidAt
        ) : null;

        var statusHistory = order.OrderStatusLogs.Select(log => new OrderStatusLogResponse(
            log.Status,
            null,
            log.ChangedAt
        )).ToList();

        return new OrderDetailResponse(
            order.Id,
            order.Id.ToString().PadLeft(8, '0'),
            order.TotalAmount,
            order.Status,
            order.ShippingMethod,
            order.CancelReason,
            items,
            address,
            payment,
            statusHistory,
            order.CreatedAt,
            order.UpdatedAt,
            order.User?.FullName
        );
    }
}