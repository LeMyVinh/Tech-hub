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

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
