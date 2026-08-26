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

    public async Task<Address?> GetByIdAsync(int id)
    {
        return await _context.Addresses.FindAsync(id);
    }

    public async Task<List<Address>> GetByUserIdAsync(int userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
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
        _context.Addresses.Update(address);
        return Task.CompletedTask;
    }

    // FIX: kiểm tra địa chỉ có đang được tham chiếu bởi bất kỳ Order nào không.
    public async Task<bool> HasOrdersAsync(int addressId)
    {
        return await _context.Orders.AnyAsync(o => o.AddressId == addressId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}