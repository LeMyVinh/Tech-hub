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

    public Task<User?> GetByIdIncludingDeletedAsync(int id) =>
        _db.Users.IgnoreQueryFilters().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    // Trang admin cần thấy cả user đã soft delete (hiển thị mờ + nút Khôi phục).
    public async Task<List<User>> GetAllAsync(int page, int pageSize) =>
        await _db.Users.IgnoreQueryFilters().Include(u => u.Role)
            .OrderBy(u => u.IsDeleted)
            .ThenByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountAsync() =>
        await _db.Users.IgnoreQueryFilters().CountAsync();

    // Đếm số Admin CHƯA bị xóa mềm (IsDeleted = false) — dùng để chặn xóa nốt
    // Admin cuối cùng của hệ thống.
    public async Task<int> GetActiveAdminCountAsync() =>
        await _db.Users.CountAsync(u => u.Role.Name == "Admin" && !u.IsDeleted);

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);

    // SOFT DELETE: ghi trực tiếp DB qua ExecuteUpdateAsync để chắc chắn IsDeleted=true
    // được lưu (tránh lỗi tracking/sentinel của EF với cột bool có default).
    public async Task SoftDeleteAsync(User user)
    {
        var now = DateTime.UtcNow;
        var rows = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.Id == user.Id && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.IsDeleted, true)
                .SetProperty(u => u.DeletedAt, now)
                .SetProperty(u => u.UpdatedAt, now));

        if (rows == 0 && !user.IsDeleted)
            throw new InvalidOperationException($"Không thể soft delete user #{user.Id}.");
    }

    // RESTORE: ghi trực tiếp DB, bỏ HasQueryFilter để tìm user đã xóa.
    public async Task RestoreAsync(User user)
    {
        var now = DateTime.UtcNow;
        var rows = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.Id == user.Id && u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.IsDeleted, false)
                .SetProperty(u => u.DeletedAt, (DateTime?)null)
                .SetProperty(u => u.UpdatedAt, now));

        if (rows == 0 && user.IsDeleted)
            throw new InvalidOperationException($"Không thể khôi phục user #{user.Id}.");
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
