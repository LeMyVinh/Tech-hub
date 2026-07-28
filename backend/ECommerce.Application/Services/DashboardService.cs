namespace ECommerce.Application;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _dashboardRepository.GetRawDataAsync(start, end);

        var revenueGrowth = data.PreviousPeriodRevenue > 0
            ? (int)((data.CurrentPeriodRevenue - data.PreviousPeriodRevenue) / data.PreviousPeriodRevenue * 100)
            : 0;

        var orderGrowth = data.PreviousPeriodOrders > 0
            ? (int)((data.CurrentPeriodOrders - data.PreviousPeriodOrders) / (decimal)data.PreviousPeriodOrders * 100)
            : 0;

        return new DashboardSummaryResponse(
            data.CurrentPeriodRevenue,
            data.CurrentPeriodOrders,
            data.TotalCustomers,
            data.TotalActiveProducts,
            data.PendingOrders,
            revenueGrowth,
            orderGrowth
        );
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _dashboardRepository.GetRawDataAsync(start, end);

        // Daily revenue
        var dailyRevenue = data.OrdersWithItems
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new RevenueByDate(
                g.Key,
                g.Sum(o => o.TotalAmount),
                g.Count()
            ))
            .OrderBy(r => r.Date)
            .ToList();

        // Revenue by category
        var categoryRevenue = data.OrdersWithItems
            .SelectMany(o => o.Items)
            .GroupBy(i => i.CategoryName)
            .Select(g => new RevenueByCategory(
                g.Key,
                g.Sum(i => i.UnitPrice * i.Quantity),
                g.Count(),
                0
            ))
            .OrderByDescending(c => c.Revenue)
            .ToList();

        var totalCategoryRevenue = categoryRevenue.Sum(c => c.Revenue);
        categoryRevenue = categoryRevenue.Select(c => c with
        {
            Percentage = totalCategoryRevenue > 0 ? (double)(c.Revenue / totalCategoryRevenue * 100) : 0
        }).ToList();

        var totalRevenue = data.CurrentPeriodRevenue;
        var averageOrderValue = data.CurrentPeriodOrders > 0 ? totalRevenue / data.CurrentPeriodOrders : 0;

        return new RevenueReportResponse(
            dailyRevenue,
            categoryRevenue,
            totalRevenue,
            averageOrderValue
        );
    }

    public async Task<TopProductsResponse> GetTopProductsAsync(int limit, DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _dashboardRepository.GetRawDataAsync(start, end);

        var topProducts = data.OrdersWithItems
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProduct(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.UnitPrice * i.Quantity),
                g.First().ImageUrl
            ))
            .OrderByDescending(p => p.TotalSold)
            .Take(limit)
            .ToList();

        return new TopProductsResponse(topProducts);
    }

    public async Task<InventoryReportResponse> GetInventoryReportAsync()
    {
        var stockData = await _dashboardRepository.GetProductStockDataAsync();

        var totalVariants = stockData.Count;
        var inStock = stockData.Count(v => v.StockQuantity > 10);
        var lowStock = stockData.Count(v => v.StockQuantity > 0 && v.StockQuantity <= 10);
        var outOfStock = stockData.Count(v => v.StockQuantity == 0);

        var lowStockProducts = stockData
            .Where(v => v.StockQuantity <= 10)
            .Select(v => new LowStockProduct(
                v.VariantId,
                v.ProductName,
                v.VariantName,
                v.StockQuantity,
                v.Sku
            ))
            .ToList();

        return new InventoryReportResponse(
            totalVariants,
            inStock,
            lowStock,
            outOfStock,
            lowStockProducts
        );
    }
}
