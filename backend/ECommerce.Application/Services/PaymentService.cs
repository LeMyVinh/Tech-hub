using ECommerce.Domain;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IVnpayService _vnpayService;
    private readonly IOrderConfirmationEmailSender _emailSender;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IVnpayService vnpayService,
        IOrderConfirmationEmailSender emailSender,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _vnpayService = vnpayService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request, string clientIp, string returnUrl)
    {
        if (request.Method != "COD" && request.Method != "VNPay")
            throw new PaymentException(400, "Phương thức thanh toán không hỗ trợ.");

        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        if (order is null || order.UserId != userId)
            throw new PaymentException(400, "Đơn hàng không hợp lệ.");

        if (order.Status != "Pending")
            throw new PaymentException(400, "Đơn hàng không ở trạng thái chờ thanh toán.");

        var existingPayment = await _paymentRepository.GetByOrderIdAsync(request.OrderId);
        // Chỉ chặn khi đơn đã có thanh toán THÀNH CÔNG/ĐANG CHỜ; nếu lần trước Failed thì cho phép thanh toán lại
        // (Payment.OrderId là UNIQUE nên tái sử dụng lại record cũ thay vì tạo bản ghi mới).
        if (existingPayment is not null && existingPayment.Status != "Failed")
            throw new PaymentException(400, "Đơn hàng đã có thanh toán.");

        if (request.Method == "COD")
        {
            var payment = existingPayment ?? new Payment { OrderId = request.OrderId, CreatedAt = DateTime.UtcNow };
            payment.Method = "COD";
            payment.Status = "Success";
            payment.TransactionCode = null;
            payment.PaidAt = DateTime.UtcNow;

            if (existingPayment is null)
                await _paymentRepository.AddAsync(payment);

            // COD tự động xác nhận đơn ngay sau khi đặt (BR-05)
            order.Status = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;
            order.OrderStatusLogs.Add(new OrderStatusLog
            {
                Status = "Confirmed",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = userId
            });

            await _paymentRepository.SaveChangesAsync();
            return MapToResponse(payment, order.TotalAmount, null);
        }

        // VNPay: tạo/khởi tạo lại Payment ở trạng thái Pending, sinh paymentUrl để redirect Customer sang VNPay
        var vnpayPayment = existingPayment ?? new Payment { OrderId = request.OrderId, CreatedAt = DateTime.UtcNow };
        vnpayPayment.Method = "VNPay";
        vnpayPayment.Status = "Pending";
        vnpayPayment.TransactionCode = null;
        vnpayPayment.PaidAt = null;

        if (existingPayment is null)
            await _paymentRepository.AddAsync(vnpayPayment);

        await _paymentRepository.SaveChangesAsync();

        var paymentUrl = _vnpayService.CreatePaymentUrl(new VnpayCreateRequest(
            order.Id,
            order.Id.ToString(),
            (long)order.TotalAmount,
            $"Thanh toan don hang {order.Id}",
            string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
            returnUrl));

        return MapToResponse(vnpayPayment, order.TotalAmount, paymentUrl);
    }

    public async Task<PaymentResponse> ProcessVnpayCallbackAsync(VnpayCallbackRequest request)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["vnp_TmnCode"] = request.vnp_TmnCode ?? "",
            ["vnp_Amount"] = request.vnp_Amount ?? "",
            ["vnp_BankCode"] = request.vnp_BankCode ?? "",
            ["vnp_BankTranNo"] = request.vnp_BankTranNo ?? "",
            ["vnp_CardType"] = request.vnp_CardType ?? "",
            ["vnp_OrderInfo"] = request.vnp_OrderInfo ?? "",
            ["vnp_PayDate"] = request.vnp_PayDate ?? "",
            ["vnp_ResponseCode"] = request.vnp_ResponseCode ?? "",
            ["vnp_TxnRef"] = request.vnp_TxnRef ?? "",
            ["vnp_TransactionNo"] = request.vnp_TransactionNo ?? "",
            ["vnp_TransactionStatus"] = request.vnp_TransactionStatus ?? "",
            ["vnp_SecureHash"] = request.vnp_SecureHash ?? "",
        };

        if (!_vnpayService.ValidateSignature(queryParams))
        {
            // ERR402-02: checksum không hợp lệ -> KHÔNG cập nhật trạng thái Payment/Order, chỉ ghi log cảnh báo.
            Console.WriteLine($"[VNPay] Invalid checksum for OrderId={request.vnp_TxnRef}");
            throw new PaymentException(400, "Chữ ký callback không hợp lệ.");
        }

        if (!int.TryParse(request.vnp_TxnRef, out var orderId))
            throw new PaymentException(400, "Đơn hàng không hợp lệ.");

        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId)
            ?? throw new PaymentException(404, "Thanh toán không tồn tại.");

        var isSuccess = request.vnp_ResponseCode == "00";

        if (isSuccess)
        {
            payment.Status = "Success";
            payment.TransactionCode = request.vnp_TransactionNo;
            payment.PaidAt = DateTime.UtcNow;

            order.Status = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;
            order.OrderStatusLogs.Add(new OrderStatusLog
            {
                Status = "Confirmed",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = order.UserId
            });
        }
        else
        {
            // ERR402-03: vnp_ResponseCode khác '00' -> Payment=Failed, Order giữ Pending để Customer thanh toán lại.
            payment.Status = "Failed";
            payment.TransactionCode = request.vnp_TransactionNo;
        }

        await _paymentRepository.SaveChangesAsync();

        // === Gửi email xác nhận cho khách hàng ngay sau khi chuyển khoản VNPay thành công ===
        // Không bọc trong cùng transaction với thanh toán: nếu gửi mail lỗi (SMTP down, mạng lỗi...)
        // thì đơn hàng/thanh toán đã xác nhận thành công vẫn được giữ nguyên, chỉ log lại lỗi gửi mail.
        if (isSuccess)
        {
            try
            {
                var orderWithDetails = await _orderRepository.GetByIdWithDetailsAsync(orderId);
                if (orderWithDetails is not null)
                {
                    await _emailSender.SendPaymentSuccessEmailAsync(orderWithDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email xác nhận thanh toán thất bại cho Order #{OrderId}", orderId);
            }
        }

        return MapToResponse(payment, order.TotalAmount, null);
    }

    public async Task<PaymentResponse?> GetPaymentByOrderIdAsync(int userId, int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId)
            return null;

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment is null)
            return null;

        return MapToResponse(payment, order.TotalAmount, null);
    }

    private static PaymentResponse MapToResponse(Payment payment, decimal amount, string? paymentUrl)
    {
        return new PaymentResponse(
            payment.Id,
            payment.Method,
            amount,
            payment.Status,
            payment.TransactionCode,
            payment.PaidAt,
            paymentUrl
        );
    }
}