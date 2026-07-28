namespace ECommerce.Application;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(int userId, CreatePaymentRequest request);
    Task<PaymentResponse> ProcessVnpayCallbackAsync(VnpayCallbackRequest request);
    Task<PaymentResponse?> GetPaymentByOrderIdAsync(int userId, int orderId);
}
