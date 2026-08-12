using ECommerce.Domain;

namespace ECommerce.Application;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token);
    Task<EmailVerificationToken?> GetByTokenAsync(string token);
    Task InvalidateActiveTokensByUserIdAsync(int userId);
    Task SaveChangesAsync();
}