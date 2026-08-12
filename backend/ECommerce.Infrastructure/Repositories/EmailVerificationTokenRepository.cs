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

    public Task<EmailVerificationToken?> GetByTokenAsync(string token) =>
        _db.EmailVerificationTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token);

    // Đóng toàn bộ token xác thực còn hiệu lực (chưa dùng, chưa hết hạn) của user
    // bằng 1 câu UPDATE duy nhất, tránh nhiều token sống song song khi user bấm
    // "gửi lại" nhiều lần.
    public async Task InvalidateActiveTokensByUserIdAsync(int userId)
    {
        await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsUsed, true));
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}