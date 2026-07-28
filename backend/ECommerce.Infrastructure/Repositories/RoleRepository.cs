using ECommerce.Application;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;
    public RoleRepository(AppDbContext db) => _db = db;

    public async Task<int> GetRoleIdByNameAsync(string name)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role == null)
            throw new InvalidOperationException($"Role '{name}' không tồn tại trong DB");
        return role.Id;
    }
}