using ECommerce.Domain;

namespace ECommerce.Application;

public interface IOrderConfirmationEmailSender
{
    /// <summary>Gửi email xác nhận cho khách hàng ngay sau khi thanh toán VNPay thành công.</summary>
    Task SendPaymentSuccessEmailAsync(Order order, CancellationToken cancellationToken = default);
}