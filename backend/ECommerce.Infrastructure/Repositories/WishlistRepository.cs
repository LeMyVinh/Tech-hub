using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly Data.AppDbContext _context;

    public WishlistRepository(Data.AppDbContext context)
    {
        _context = context;
    }
    

    public async Task<List<WishlistItem>> GetByUserIdAsync(int userId)
    {
        return await _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages)
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductVariants)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId)
    {
        return await _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages)
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductVariants)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<bool> TryAddAsync(WishlistItem item)
    {
        try
        {
            await _context.WishlistItems.AddAsync(item);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }
    public async Task AddAsync(WishlistItem item)
{
    await _context.WishlistItems.AddAsync(item);
    await _context.SaveChangesAsync();
}

    public Task RemoveAsync(WishlistItem item)
    {
        _context.WishlistItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}