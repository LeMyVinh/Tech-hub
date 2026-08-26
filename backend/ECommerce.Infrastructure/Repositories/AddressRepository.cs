using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly Data.AppDbContext _context;

    public AddressRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Address?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = _context.Addresses.Where(a => a.Id == id);
        if (!includeDeleted) query = query.Where(a => !a.IsDeleted);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<Address>> GetByUserIdAsync(int userId, bool includeDeleted = false)
    {
        var query = _context.Addresses.Where(a => a.UserId == userId);
        if (!includeDeleted) query = query.Where(a => !a.IsDeleted);

        return await query
            .OrderBy(a => a.IsDeleted)
            .ThenByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Address address)
    {
        await _context.Addresses.AddAsync(address);
    }

    public Task UpdateAsync(Address address)
    {
        _context.Addresses.Update(address);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(Address address)
    {
        address.IsDeleted = true;
        address.DeletedAt = DateTime.UtcNow;
        address.IsDefault = false;
        _context.Addresses.Update(address);
        return Task.CompletedTask;
    }

    public Task RestoreAsync(Address address)
    {
        address.IsDeleted = false;
        address.DeletedAt = null;
        address.IsDefault = false;
        _context.Addresses.Update(address);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
