using ECommerce.Domain;

namespace ECommerce.Application;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;

    public PaymentService(IPaymentRepository paymentRepository, IOrderRepository orderRepository)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        if (order.UserId != userId)
            throw new PaymentException(403, "Bạn không có quyền thanh toán đơn hàng này.");

        if (order.Status != "Pending")
            throw new PaymentException(400, "Đơn hàng không ở trạng thái chờ thanh toán.");

        var existingPayment = await _paymentRepository.GetByOrderIdAsync(request.OrderId);
        if (existingPayment is not null)
            throw new PaymentException(400, "Đơn hàng đã có thanh toán.");

        if (request.Method == "COD")
        {
            // COD: auto-confirm payment
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Method = "COD",
                Status = "Success",
                TransactionCode = null,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            // Update order status to Confirmed
            order.Status = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;
            order.OrderStatusLogs.Add(new OrderStatusLog
            {
                Status = "Confirmed",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = userId
            });

            await _paymentRepository.SaveChangesAsync();
            return MapToResponse(payment, order.TotalAmount);
        }
        else if (request.Method == "VNPay")
        {
            // VNPay: create pending payment, redirect to VNPay gateway
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Method = "VNPay",
                Status = "Pending",
                TransactionCode = null,
                PaidAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // In real implementation, generate VNPay payment URL here
            return MapToResponse(payment, order.TotalAmount);
        }
        else
        {
            throw new PaymentException(400, "Phương thức thanh toán không hỗ trợ.");
        }
    }

    public async Task<PaymentResponse> ProcessVnpayCallbackAsync(VnpayCallbackRequest request)
    {
        // In real implementation, verify VNPay signature here
        // For now, process based on response code
        var orderId = int.Parse(request.vnp_TxnRef);
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new PaymentException(404, "Đơn hàng không tồn tại.");

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId)
            ?? throw new PaymentException(404, "Thanh toán không tồn tại.");

        if (request.vnp_ResponseCode == "00")
        {
            // Success
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
            // Failed
            payment.Status = "Failed";
            payment.TransactionCode = request.vnp_TransactionNo;
        }

        await _paymentRepository.SaveChangesAsync();
        return MapToResponse(payment, order.TotalAmount);
    }

    public async Task<PaymentResponse?> GetPaymentByOrderIdAsync(int userId, int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId)
            return null;

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        if (payment is null)
            return null;

        return MapToResponse(payment, order.TotalAmount);
    }

    private static PaymentResponse MapToResponse(Payment payment, decimal amount)
    {
        return new PaymentResponse(
            payment.Id,
            payment.Method,
            amount,
            payment.Status,
            payment.TransactionCode,
            payment.PaidAt
        );
    }
}
