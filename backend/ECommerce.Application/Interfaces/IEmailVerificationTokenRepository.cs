using ECommerce.Domain;

namespace ECommerce.Application;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token);

    /// <summary>Tra cứu mã OTP còn hiệu lực (chưa dùng, chưa hết hạn) theo đúng user + mã nhập vào.</summary>
    Task<EmailVerificationToken?> GetActiveByUserIdAndCodeAsync(int userId, string code);

    Task InvalidateActiveTokensByUserIdAsync(int userId);
    Task SaveChangesAsync();
}