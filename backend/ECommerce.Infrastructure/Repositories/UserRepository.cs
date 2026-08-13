using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<List<User>> GetAllAsync(int page, int pageSize) =>
        await _db.Users.Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountAsync() =>
        await _db.Users.CountAsync();

    // FIX: đếm số Admin đang hoạt động (IsActive = true), dùng để chặn khóa nốt
    // Admin cuối cùng của hệ thống.
    public async Task<int> GetActiveAdminCountAsync() =>
        await _db.Users.CountAsync(u => u.Role.Name == "Admin" && u.IsActive == true);

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}