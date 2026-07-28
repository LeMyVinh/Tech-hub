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

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
