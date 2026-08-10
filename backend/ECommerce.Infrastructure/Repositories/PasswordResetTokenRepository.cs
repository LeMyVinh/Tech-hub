using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;
    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token) => await _db.PasswordResetTokens.AddAsync(token);

    public Task<PasswordResetToken?> GetByTokenAsync(string token) =>
        _db.PasswordResetTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    // SECURITY FIX: đóng toàn bộ token reset còn hiệu lực (chưa dùng, chưa hết hạn)
    // của user bằng 1 câu UPDATE duy nhất, tránh trường hợp nhiều token sống song song.
    public async Task InvalidateActiveTokensByUserIdAsync(int userId)
    {
        await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsUsed, true));
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}