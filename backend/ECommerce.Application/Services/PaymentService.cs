using ECommerce.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Application;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IVnpayService _vnpayService;
    private readonly IStripeService _stripeService;
    private readonly IOrderConfirmationEmailSender _emailSender;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IVnpayService vnpayService,
        IStripeService stripeService,
        IOrderConfirmationEmailSender emailSender,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _vnpayService = vnpayService;
        _stripeService = stripeService;
        _emailSender = emailSender;
        _logger = logger;
    }

    // ==========================================================================
    // COD / VNPay (giữ nguyên logic cũ)
    // ==========================================================================

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
        if (existingPayment is not null && existingPayment.Status != "Failed")
            throw new PaymentException(400, "Đơn hàng đã có thanh toán.");

        if (request.Method == "COD")
        {
            var payment = existingPayment ?? new Payment { OrderId = request.OrderId, CreatedAt = DateTime.UtcNow };
            payment.Method = "COD";
            payment.Status = "Success";
            payment.TransactionCode = null;
            payment.TransactionDate = null;
            payment.GatewayPaymentIntentId = null;
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
        vnpayPayment.GatewayPaymentIntentId = null;
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
            _logger.LogWarning("[VNPay] Invalid checksum for OrderId={OrderId}", request.vnp_TxnRef);
            throw new PaymentException(400, "Chữ ký callback không hợp lệ.");
        }

        if (!int.TryParse(request.vnp_TxnRef, out var orderId))
            throw new PaymentException(400, "Đơn hàng không hợp lệ.");

        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId)
            ?? throw new PaymentException(404, "Thanh toán không tồn tại.");

        // Idempotent: nếu đã Success rồi thì không xử lý lại (tránh double-confirm khi
        // người dùng F5 trang payment-result hoặc VNPay gọi callback nhiều lần).
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

    // ==========================================================================
    // Credit Card (Stripe)
    // ==========================================================================

    public async Task<CreditCardPaymentResponse> CreateCreditCardPaymentAsync(int userId, CreateCreditCardPaymentRequest request)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        // IDOR guard
        if (order.UserId != userId)
            throw new PaymentException(403, "Bạn không có quyền thanh toán đơn hàng này.");

        if (order.Status != "Pending")
            throw new PaymentException(400, "Đơn hàng không ở trạng thái chờ thanh toán.");

        var existingPayment = await _paymentRepository.GetByOrderIdAsync(request.OrderId);

        // Chặn thanh toán lại đơn đã Success
        if (existingPayment is not null && existingPayment.Status == "Success")
            throw new PaymentException(400, "Đơn hàng này đã được thanh toán.");

        // Tái sử dụng PaymentIntent Pending còn hiệu lực (double-click / F5) thay vì tạo mới
        if (existingPayment is not null
            && existingPayment.Method == "CreditCard"
            && existingPayment.Status == "Pending"
            && !string.IsNullOrEmpty(existingPayment.GatewayPaymentIntentId))
        {
            StripePaymentIntentResult? current = null;
            try
            {
                current = await _stripeService.RetrievePaymentIntentAsync(existingPayment.GatewayPaymentIntentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể retrieve PaymentIntent cũ {Id}, sẽ tạo mới.", existingPayment.GatewayPaymentIntentId);
            }

            if (current is not null &&
                current.Status is "requires_payment_method" or "requires_confirmation" or "requires_action")
            {
                return new CreditCardPaymentResponse(
                    existingPayment.Id, current.PaymentIntentId, current.ClientSecret, _stripeService.PublishableKey);
            }
        }

        // KHÔNG tin amount từ FE — luôn lấy từ DB
        var amountVnd = (long)order.TotalAmount;
        var idempotencyKey = $"order-{order.Id}-credit-card";

        StripePaymentIntentResult intentResult;
        try
        {
            intentResult = await _stripeService.CreatePaymentIntentAsync(
                new StripeCreatePaymentIntentRequest(order.Id, amountVnd, "vnd", order.User.Email, idempotencyKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tạo Stripe PaymentIntent thất bại cho Order #{OrderId}", order.Id);
            throw new PaymentException(502, "Không thể khởi tạo thanh toán, vui lòng thử lại sau.");
        }

        var payment = existingPayment ?? new Payment { OrderId = order.Id, CreatedAt = DateTime.UtcNow };
        payment.Method = "CreditCard";
        payment.Status = "Pending";
        payment.GatewayPaymentIntentId = intentResult.PaymentIntentId;
        payment.TransactionCode = null;
        payment.TransactionDate = null;
        payment.PaidAt = null;

        if (existingPayment is null)
            await _paymentRepository.AddAsync(payment);

        await _paymentRepository.SaveChangesAsync();

        return new CreditCardPaymentResponse(
            payment.Id, intentResult.PaymentIntentId, intentResult.ClientSecret,_stripeService.PublishableKey);
    }

    public async Task HandleStripeWebhookAsync(string rawJson, string signatureHeader)
    {
        // Verify chữ ký Stripe-Signature — request không hợp lệ coi như có thể bị giả mạo.
        if (!_stripeService.TryConstructEvent(rawJson, signatureHeader, out var evt) || evt is null)
        {
            _logger.LogWarning("[Stripe Webhook] Chữ ký không hợp lệ hoặc event không phải PaymentIntent.");
            throw new PaymentException(400, "Webhook không hợp lệ.");
        }

        if (evt.EventType is not ("payment_intent.succeeded" or "payment_intent.payment_failed"))
            return;

        if (!int.TryParse(evt.OrderIdFromMetadata, out var orderId))
        {
            _logger.LogWarning("[Stripe Webhook] PaymentIntent {Id} thiếu metadata OrderId.", evt.PaymentIntentId);
            return;
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order is null)
        {
            _logger.LogWarning("[Stripe Webhook] Order #{OrderId} không tồn tại.", orderId);
            return;
        }

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment is null || payment.GatewayPaymentIntentId != evt.PaymentIntentId)
        {
            _logger.LogWarning(
                "[Stripe Webhook] Payment không khớp PaymentIntentId cho Order #{OrderId}. Expected={Expected}, Got={Got}",
                orderId, payment?.GatewayPaymentIntentId, evt.PaymentIntentId);
            return;
        }

        // Idempotent: Stripe có thể gửi webhook trùng (retry) — nếu đã Success thì bỏ qua,
        // tránh cập nhật lại / gửi email xác nhận 2 lần.
        if (payment.Status == "Success")
            return;

        if (evt.EventType == "payment_intent.payment_failed")
        {
            payment.Status = "Failed";
            await _paymentRepository.SaveChangesAsync();
            return;
        }

        // Đối chiếu số tiền Stripe xác nhận với số tiền thật của đơn hàng trong DB —
        // đề phòng trường hợp PaymentIntent bị thao túng ở đâu đó ngoài tầm kiểm soát.
        if (evt.AmountReceived != (long)order.TotalAmount)
        {
            _logger.LogError(
                "[Stripe Webhook] Amount mismatch Order #{OrderId}: DB={DbAmount}, Stripe={StripeAmount}",
                orderId, order.TotalAmount, evt.AmountReceived);
            payment.Status = "Failed";
            await _paymentRepository.SaveChangesAsync();
            return;
        }

        payment.Status = "Success";
        payment.TransactionCode = evt.PaymentIntentId;
        payment.PaidAt = DateTime.UtcNow;

        order.Status = "Confirmed";
        order.UpdatedAt = DateTime.UtcNow;
        order.OrderStatusLogs.Add(new OrderStatusLog
        {
            Status = "Confirmed",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = order.UserId
        });

        await _paymentRepository.SaveChangesAsync();

        // Lỗi gửi mail không được làm webhook fail (Stripe sẽ retry vô hạn nếu response != 2xx),
        // nên chỉ log, không throw.
        try
        {
            await _emailSender.SendPaymentSuccessEmailAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email xác nhận thanh toán thất bại cho Order #{OrderId}", orderId);
        }
    }

    // ==========================================================================
    // Chung
    // ==========================================================================

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
    /// Hoàn tiền khi hủy đơn đã thanh toán thành công — hỗ trợ cả VNPay lẫn Credit Card (Stripe).
    /// </summary>
    public async Task RefundIfPaidAsync(int orderId, string createBy = "system", string clientIp = "127.0.0.1")
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment is null || payment.Status != "Success")
            return;

        if (payment.Method == "VNPay")
        {
            await RefundVnpayAsync(order, payment, createBy, clientIp);
        }
        else if (payment.Method == "CreditCard")
        {
            await RefundCreditCardAsync(order, payment);
        }
        // COD: không có gì để hoàn qua gateway, xử lý hoàn tiền thủ công ngoài hệ thống.
    }

    private async Task RefundVnpayAsync(Order order, Payment payment, string createBy, string clientIp)
    {
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
                order.Id, result.ResponseCode, result.Message);

            throw new PaymentException(400,
                $"Hoàn tiền VNPay thất bại ({result.ResponseCode}): {result.Message}");
        }

        payment.Status = "Refunded";
        payment.RefundResponseId = result.ResponseId;
        await _paymentRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Order #{OrderId}: hoàn tiền VNPay thành công. ResponseId={ResponseId}",
            order.Id, result.ResponseId);
    }

    private async Task RefundCreditCardAsync(Order order, Payment payment)
    {
        if (string.IsNullOrWhiteSpace(payment.GatewayPaymentIntentId))
        {
            _logger.LogError("Không thể hoàn tiền Credit Card Order #{OrderId}: thiếu GatewayPaymentIntentId.", order.Id);
            throw new PaymentException(400, "Thiếu thông tin giao dịch Stripe để hoàn tiền.");
        }

        var result = await _stripeService.RefundAsync(payment.GatewayPaymentIntentId, (long)order.TotalAmount);

        if (!result.Success)
        {
            _logger.LogError(
                "Stripe refund thất bại Order #{OrderId}: {Message}", order.Id, result.Message);

            throw new PaymentException(400, $"Hoàn tiền thất bại: {result.Message}");
        }

        payment.Status = "Refunded";
        payment.RefundResponseId = result.RefundId;
        await _paymentRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Order #{OrderId}: hoàn tiền Stripe thành công. RefundId={RefundId}",
            order.Id, result.RefundId);
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