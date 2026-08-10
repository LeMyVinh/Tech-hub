using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token) => await _db.RefreshTokens.AddAsync(token);

    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        _db.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.Token == token);

    // AUTH-117 fix: single atomic UPDATE, bypassing the change tracker, so the
    // database (not application code) decides who wins the race.
    public async Task<bool> TryRevokeAsync(long id)
    {
        var affected = await _db.RefreshTokens
            .Where(t => t.Id == id && !t.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true));

        return affected > 0;
    }

    // SECURITY FIX: thu hồi hàng loạt bằng 1 câu UPDATE, không cần load entity vào
    // change tracker -- an toàn kể cả khi user có rất nhiều refresh token (nhiều
    // thiết bị/trình duyệt).
    public async Task RevokeAllByUserIdAsync(int userId)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true));
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}