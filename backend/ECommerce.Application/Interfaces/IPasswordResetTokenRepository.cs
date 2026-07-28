using ECommerce.Domain;

namespace ECommerce.Application;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task SaveChangesAsync();
}
