using ECommerce.Domain;

namespace ECommerce.Application;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task SaveChangesAsync();
}
