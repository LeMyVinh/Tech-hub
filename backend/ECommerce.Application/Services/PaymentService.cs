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
        // FIX #4: Cho phép retry khi Status == "Pending" hoặc "Failed" (trước đây chỉ Failed).
        // Khi khách đóng tab VNPay, payment còn Pending -> retry được.
        if (existingPayment is not null && existingPayment.Status != "Failed" && existingPayment.Status != "Pending")
            throw new PaymentException(400, "Đơn hàng đã có thanh toán.");

        if (request.Method == "COD")
        {
            var payment = existingPayment ?? new Payment { OrderId = request.OrderId, CreatedAt = DateTime.UtcNow };
            payment.Method = "COD";
            payment.Status = "Success";
            payment.TransactionCode = null;
            payment.TransactionDate = null;
            payment.PaidAt = DateTime.UtcNow;

            if (existingPayment is null)
                await _paymentRepository.AddAsync(payment);

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

        var vnpayPayment = existingPayment ?? new Payment { OrderId = request.OrderId, CreatedAt = DateTime.UtcNow };
        vnpayPayment.Method = "VNPay";
        vnpayPayment.Status = "Pending";
        vnpayPayment.TransactionCode = null;
        vnpayPayment.TransactionDate = null;
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
            Console.WriteLine($"[VNPay] Invalid checksum for OrderId={request.vnp_TxnRef}");
            throw new PaymentException(400, "Chữ ký callback không hợp lệ.");
        }

        if (!int.TryParse(request.vnp_TxnRef, out var orderId))
            throw new PaymentException(400, "Đơn hàng không hợp lệ.");

        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId)
            ?? throw new PaymentException(404, "Thanh toán không tồn tại.");

        // FIX #6: Idempotent – nếu đã Success rồi thì return luôn, tránh gửi email trùng + log trạng thái trùng khi VNPay retry IPN hoặc user reload.
        if (payment.Status == "Success")
            return MapToResponse(payment, order.TotalAmount, null);

        var isSuccess = request.vnp_ResponseCode == "00";

        if (isSuccess)
        {
            payment.Status = "Success";
            payment.TransactionCode = request.vnp_TransactionNo;
            payment.TransactionDate = request.vnp_PayDate; // yyyyMMddHHmmss — cần cho Refund API
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
            payment.Status = "Failed";
            payment.TransactionCode = request.vnp_TransactionNo;
        }

        await _paymentRepository.SaveChangesAsync();

        if (isSuccess)
        {
            try
            {
                var orderWithDetails = await _orderRepository.GetByIdWithDetailsAsync(orderId);
                if (orderWithDetails is not null)
                    await _emailSender.SendPaymentSuccessEmailAsync(orderWithDetails);
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

    /// <summary>
    /// Gọi VNPay Refund API khi hủy đơn đã thanh toán thành công.
    /// Sandbox có thể bị hạn chế — nếu fail sẽ throw PaymentException.
    /// </summary>
    public async Task RefundIfPaidAsync(int orderId, string createBy = "system", string clientIp = "127.0.0.1")
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment is null)
            return;

        if (payment.Method != "VNPay" || payment.Status != "Success")
            return;

        var txnDate = payment.TransactionDate;
        if (string.IsNullOrWhiteSpace(txnDate) && payment.PaidAt.HasValue)
            txnDate = payment.PaidAt.Value.AddHours(7).ToString("yyyyMMddHHmmss");

        if (string.IsNullOrWhiteSpace(txnDate))
            throw new PaymentException(400, "Thiếu TransactionDate (vnp_PayDate) để gọi hoàn tiền VNPay.");

        var result = await _vnpayService.RefundAsync(new VnpayRefundRequest(
            TxnRef: order.Id.ToString(),
            AmountVnd: (long)order.TotalAmount,
            TransactionNo: payment.TransactionCode ?? "0",
            TransactionDate: txnDate,
            CreateBy: string.IsNullOrWhiteSpace(createBy) ? "admin" : createBy,
            ClientIp: string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
            OrderInfo: $"Hoan tien don hang {order.Id}",
            FullRefund: true
        ));

        if (!result.Success)
        {
            _logger.LogError(
                "VNPay refund thất bại Order #{OrderId}: Code={Code}, Message={Message}",
                orderId, result.ResponseCode, result.Message);

            throw new PaymentException(400,
                $"Hoàn tiền VNPay thất bại ({result.ResponseCode}): {result.Message}");
        }

        payment.Status = "Refunded";
        payment.RefundResponseId = result.ResponseId;
        await _paymentRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Order #{OrderId}: hoàn tiền VNPay thành công. ResponseId={ResponseId}",
            orderId, result.ResponseId);
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