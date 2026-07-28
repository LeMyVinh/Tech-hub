using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly Data.AppDbContext _context;

    public CartRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        await _context.Carts.AddAsync(cart);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
