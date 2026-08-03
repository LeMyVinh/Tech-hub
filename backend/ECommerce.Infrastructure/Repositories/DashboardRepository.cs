using ECommerce.Application;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardRawData> GetRawDataAsync(DateTime startDate, DateTime endDate)
    {
        var previousStart = startDate.AddDays(-(endDate - startDate).Days);

        var currentRevenue = await _context.Orders
            .Where(o => o.Status != "Cancelled" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .SumAsync(o => o.TotalAmount);

        var previousRevenue = await _context.Orders
            .Where(o => o.Status != "Cancelled" && o.CreatedAt >= previousStart && o.CreatedAt < startDate)
            .SumAsync(o => o.TotalAmount);

        var currentOrders = await _context.Orders
            .CountAsync(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        var previousOrders = await _context.Orders
            .CountAsync(o => o.CreatedAt >= previousStart && o.CreatedAt < startDate);

        var totalCustomers = await _context.Users.CountAsync(u => u.Role.Name == "Customer");
        var totalProducts = await _context.Products.CountAsync(p => p.Status == "Active");
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");

        var ordersWithItems = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
            .Where(o => o.Status != "Cancelled" && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .ToListAsync();

        var result = ordersWithItems.Select(o => new OrderWithItems(
            o.CreatedAt,
            o.TotalAmount,
            o.Status,
            o.OrderItems.Select(i => new OrderItemData(
                i.ProductVariant.ProductId,
                i.ProductVariant.Product.Name,
                i.ProductVariant.Product.Category?.Name ?? "Chưa phân loại",
                i.Quantity,
                i.UnitPrice,
                null
            )).ToList()
        )).ToList();

        return new DashboardRawData(
            currentRevenue,
            previousRevenue,
            currentOrders,
            previousOrders,
            totalCustomers,
            totalProducts,
            pendingOrders,
            result
        );
    }

    public async Task<List<ProductStockData>> GetProductStockDataAsync()
    {
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .OrderBy(v => v.StockQuantity)
            .ToListAsync();

        return variants.Select(v => new ProductStockData(
            v.Id,
            v.ProductId,
            v.Product.Name,
            v.VariantName,
            v.Sku,
            v.StockQuantity
        )).ToList();
    }
}