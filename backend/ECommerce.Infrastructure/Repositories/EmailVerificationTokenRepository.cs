using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _db;
    public EmailVerificationTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(EmailVerificationToken token) => await _db.EmailVerificationTokens.AddAsync(token);

    public Task<EmailVerificationToken?> GetActiveByUserIdAndCodeAsync(int userId, string code)
    {
        return _db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.Token == code && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    // Đóng toàn bộ OTP còn hiệu lực của user bằng 1 câu UPDATE, tránh nhiều mã sống
    // song song khi user bấm "Gửi lại mã" nhiều lần.
    public async Task InvalidateActiveTokensByUserIdAsync(int userId)
    {
        await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsUsed, true));
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}