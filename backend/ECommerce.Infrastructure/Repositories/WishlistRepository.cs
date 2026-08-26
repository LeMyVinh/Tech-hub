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
    

    public async Task<List<WishlistItem>> GetByUserIdAsync(int userId, bool includeDeleted = false)
    {
        IQueryable<WishlistItem> query = _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages)
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductVariants)
            .Where(w => w.UserId == userId);
        if (!includeDeleted) query = query.Where(w => !w.IsDeleted);

        return await query
            .OrderBy(w => w.IsDeleted)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId, bool includeDeleted = false)
    {
        IQueryable<WishlistItem> query = _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages)
            .Include(w => w.Product)
                .ThenInclude(p => p.ProductVariants)
            .Where(w => w.UserId == userId && w.ProductId == productId);
        if (!includeDeleted) query = query.Where(w => !w.IsDeleted);
        return await query.FirstOrDefaultAsync();
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
        // Chỉ đưa entity vào change tracker. Service quyết định thời điểm commit để
        // có thể xử lý DbUpdateException (ví dụ request thêm trùng diễn ra đồng thời).
        await _context.WishlistItems.AddAsync(item);
    }

    public Task SoftDeleteAsync(WishlistItem item)
    {
        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        _context.WishlistItems.Update(item);
        return Task.CompletedTask;
    }

    public Task RestoreAsync(WishlistItem item)
    {
        item.IsDeleted = false;
        item.DeletedAt = null;
        _context.WishlistItems.Update(item);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
