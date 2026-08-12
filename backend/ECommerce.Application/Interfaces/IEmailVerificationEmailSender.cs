using ECommerce.Domain;

namespace ECommerce.Application;

public interface IEmailVerificationEmailSender
{
    /// <summary>Gửi mã OTP xác thực email (6 chữ số).</summary>
    Task SendAsync(User user, string otpCode, CancellationToken cancellationToken = default);
}