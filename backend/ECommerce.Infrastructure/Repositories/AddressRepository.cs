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

    public Task DeleteAsync(Address address)
    {
        _context.Addresses.Remove(address);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
