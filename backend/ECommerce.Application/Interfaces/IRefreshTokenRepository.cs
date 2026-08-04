using ECommerce.Domain;

namespace ECommerce.Application;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// AUTH-117 fix: atomically flips IsRevoked false-&gt;true for the given token id
    /// in a single UPDATE ... WHERE IsRevoked = 0 statement, and returns whether
    /// THIS call was the one that actually flipped it. If two concurrent refresh
    /// requests race on the same token, only one of them can ever get `true` back,
    /// closing the read-then-write TOCTOU window that existed with
    /// GetByTokenAsync + SaveChangesAsync.
    /// </summary>
    Task<bool> TryRevokeAsync(long id);

    Task SaveChangesAsync();
}