namespace ECommerce.Application;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request, string clientIp, string returnUrl);
    Task<PaymentResponse> ProcessVnpayCallbackAsync(VnpayCallbackRequest request);
    Task<PaymentResponse?> GetPaymentByOrderIdAsync(int userId, int orderId);
    Task RefundIfPaidAsync(int orderId, string createBy = "system", string clientIp = "127.0.0.1");
}