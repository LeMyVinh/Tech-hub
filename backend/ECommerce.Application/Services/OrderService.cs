using ECommerce.Domain;

namespace ECommerce.Application;

public class OrderService : IOrderService
{
    private const decimal StandardShippingFee = 0m;
    private const decimal ExpressShippingFee = 30000m;

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
        // 2.13: cho phép thêm "CreditCard" (Stripe) đi qua validate cùng COD/VNPay.
        if (request.PaymentMethod is not ("COD" or "VNPay" or "CreditCard"))
            throw new OrderException(400, "Phương thức thanh toán không hợp lệ.");

        var shippingFee = ResolveShippingFee(request.ShippingMethod);

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var cart = await _cartService.GetCartAsync(userId);
            if (cart.Items.Count == 0)
                throw new OrderException(400, "Giỏ hàng của bạn đang trống.");

            var address = await _addressRepository.GetByIdAsync(request.AddressId);
            if (address is null || address.UserId != userId)
                throw new OrderException(400, "Địa chỉ giao hàng không hợp lệ.");

            foreach (var item in cart.Items)
            {
                var variantCheck = await _variantRepository.GetByIdAsync(item.VariantId);
                if (variantCheck is null)
                    throw new OrderException(400, $"Sản phẩm {item.ProductName} không còn tồn tại.");
                if (variantCheck.Product.Status != "Active")
                    throw new OrderException(400, $"Sản phẩm {item.ProductName} hiện không còn kinh doanh. Vui lòng xóa khỏi giỏ hàng.");
            }

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
                // FIX (đơn hàng bị đổi theo địa chỉ mới): snapshot thông tin địa chỉ
                // ngay tại thời điểm đặt hàng, thay vì chỉ lưu AddressId. Nếu sau này
                // khách sửa lại địa chỉ này (UserService.UpdateAddressAsync), đơn hàng
                // đã đặt vẫn giữ nguyên thông tin giao hàng gốc.
                RecipientName = address.RecipientName,
                Phone = address.Phone,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                DetailAddress = address.DetailAddress,
                ShippingMethod = request.ShippingMethod,
                ShippingFee = shippingFee,
                TotalAmount = cart.TotalAmount + shippingFee,
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

            await _cartService.ClearCartAsync(userId);

            // 2.13: CreditCard (Stripe) không tạo Payment/redirect ngay ở bước này —
            // FE cần hiển thị form nhập thẻ (Stripe Elements) trước, sau đó tự gọi
            // một endpoint riêng (vd POST /payments/credit-card) để tạo PaymentIntent
            // và xác nhận thanh toán. COD/VNPay giữ nguyên hành vi cũ (redirect ngay).
            if (request.PaymentMethod != "CreditCard")
            {
                var payment = await _paymentService.CreatePaymentAsync(
                    userId,
                    new CreatePaymentRequest(order.Id, request.PaymentMethod),
                    clientIp,
                    returnUrl);

                await transaction.CommitAsync();

                return MapToResponse(order) with { PaymentUrl = payment.PaymentUrl };
            }

            await transaction.CommitAsync();

            return MapToResponse(order);
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

    // FIX (transaction khi hủy đơn): trước đây hàm này KHÔNG được bọc trong
    // transaction, trong khi bên trong vừa đổi Order.Status, vừa hoàn kho
    // (IncrementStockAsync ghi thẳng xuống DB bằng ExecuteUpdateAsync), vừa gọi
    // hoàn tiền VNPay/Stripe (RefundIfPaidAsync, có thể throw PaymentException).
    // Nếu bước hoàn tiền lỗi SAU KHI đã hoàn kho, exception văng ra ngoài trong khi
    // SaveChangesAsync() (đổi Status/CancelReason/OrderStatusLogs) chưa kịp chạy ->
    // kho đã bị cộng lại nhưng đơn hàng vẫn ở trạng thái cũ (không phải Cancelled),
    // gây sai lệch dữ liệu (thất thoát tồn kho, đơn "ma"). Bọc toàn bộ trong
    // transaction để rollback đồng bộ mọi thay đổi khi có lỗi ở bất kỳ bước nào.
    public async Task<OrderResponse> CancelOrderAsync(int userId, int orderId, string? reason)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
            ?? throw new OrderException(404, "Đơn hàng không tồn tại.");

        if (order.UserId != userId)
            throw new OrderException(403, "Bạn không có quyền hủy đơn hàng này.");

        var cancellableStatuses = new[] { "Pending", "Confirmed" };
        if (!cancellableStatuses.Contains(order.Status))
            throw new OrderException(400, "Chỉ có thể hủy đơn hàng khi chưa được xử lý / giao hàng.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            order.Status = "Cancelled";
            order.CancelReason = reason;
            order.UpdatedAt = DateTime.UtcNow;

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

            // VNPay đã thanh toán thành công → gọi Refund API
            if (order.Payment is not null
                && order.Payment.Method == "VNPay"
                && order.Payment.Status == "Success")
            {
                await _paymentService.RefundIfPaidAsync(
                    order.Id,
                    createBy: $"user:{userId}",
                    clientIp: "127.0.0.1");
            }
            else if (order.Payment is not null && order.Payment.Status == "Success")
            {
                order.Payment.Status = "Refunded";
            }

            await _orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();
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
            throw new OrderException(500, "Hủy đơn hàng thất bại, vui lòng thử lại.");
        }

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

    // FIX (transaction khi đổi trạng thái đơn): tương tự CancelOrderAsync — trước
    // đây khi Admin chuyển trạng thái sang "Cancelled" (hoàn kho + hoàn tiền) mà
    // KHÔNG có transaction bọc ngoài, một lỗi hoàn tiền giữa chừng sẽ để lại kho đã
    // hoàn nhưng Order.Status/OrderStatusLogs chưa được lưu -> dữ liệu không nhất
    // quán. Bọc toàn bộ thao tác cập nhật + hoàn kho + hoàn tiền + SaveChangesAsync
    // trong transaction, rollback toàn bộ nếu có bất kỳ lỗi nào.
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

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            order.OrderStatusLogs.Add(new OrderStatusLog
            {
                Status = request.Status,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = adminUserId
            });

            if (request.Status == "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    await _variantRepository.IncrementStockAsync(item.ProductVariantId, item.Quantity);
                }

                // VNPay đã thanh toán thành công → gọi Refund API
                if (order.Payment is not null
                    && order.Payment.Method == "VNPay"
                    && order.Payment.Status == "Success")
                {
                    await _paymentService.RefundIfPaidAsync(
                        order.Id,
                        createBy: $"admin:{adminUserId}",
                        clientIp: "127.0.0.1");
                }
                else if (order.Payment is not null && order.Payment.Status == "Success")
                {
                    order.Payment.Status = "Refunded";
                }
            }

            await _orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();
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
            throw new OrderException(500, "Cập nhật trạng thái đơn hàng thất bại, vui lòng thử lại.");
        }

        return MapToResponse(order);
    }

    private static decimal ResolveShippingFee(string shippingMethod) => shippingMethod switch
    {
        "Standard" => StandardShippingFee,
        "Express" => ExpressShippingFee,
        _ => throw new OrderException(400, "Phương thức vận chuyển không hợp lệ.")
    };

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
            order.ShippingFee,
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

        // FIX (đơn hàng bị đổi theo địa chỉ mới): đọc thông tin địa chỉ từ snapshot
        // lưu trực tiếp trên Order (RecipientName/Phone/Province/District/Ward/
        // DetailAddress), KHÔNG còn đọc từ order.Address — vì bản ghi Address có thể
        // đã bị khách chỉnh sửa sau khi đặt đơn (UserService.UpdateAddressAsync ghi
        // đè tại chỗ), khiến các đơn hàng cũ hiển thị sai địa chỉ giao hàng thực tế.
        var address = new AddressResponse(
            order.AddressId,
            order.RecipientName,
            order.Phone,
            order.DetailAddress,
            order.Ward,
            order.District,
            order.Province
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
            order.ShippingFee,
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