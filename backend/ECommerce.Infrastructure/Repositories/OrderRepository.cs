using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly Data.AppDbContext _context;

    public OrderRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Review)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.OrderStatusLogs)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// SECURITY FIX (Review IDOR): trước đây ReviewService gọi nhầm GetByIdAsync(orderItemId)
    /// -- một hàm tìm theo Order.Id -- để tra cứu OrderItem, nên không có cách nào xác minh
    /// review có thuộc đúng đơn hàng/đúng người dùng/đúng trạng thái Delivered hay không.
    /// Hàm này trả về đúng OrderItem, kèm Order (để check UserId/Status) và ProductVariant
    /// (để đối chiếu ProductId client gửi lên) trong một lần truy vấn.
    /// </summary>
    public async Task<OrderItem?> GetOrderItemWithDetailsAsync(int orderItemId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.ProductVariant)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId, int page, int pageSize)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserOrdersCountAsync(int userId)
    {
        return await _context.Orders.CountAsync(o => o.UserId == userId);
    }

    public async Task<List<Order>> GetAllOrdersAsync(int page, int pageSize, string? status)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetAllOrdersCountAsync(string? status)
    {
        var query = _context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        return await query.CountAsync();
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}