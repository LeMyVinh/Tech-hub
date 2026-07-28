namespace ECommerce.Application;

public sealed record DashboardSummaryResponse(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    int PendingOrders,
    int RevenueGrowthPercent,
    int OrderGrowthPercent
);

public sealed record RevenueReportResponse(
    List<RevenueByDate> DailyRevenue,
    List<RevenueByCategory> CategoryRevenue,
    decimal TotalRevenue,
    decimal AverageOrderValue
);

public sealed record RevenueByDate(
    DateTime Date,
    decimal Revenue,
    int OrderCount
);

public sealed record RevenueByCategory(
    string CategoryName,
    decimal Revenue,
    int OrderCount,
    double Percentage
);

public sealed record TopProductsResponse(
    List<TopProduct> Products
);

public sealed record TopProduct(
    int ProductId,
    string ProductName,
    int TotalSold,
    decimal Revenue,
    string? ImageUrl
);

public sealed record InventoryReportResponse(
    int TotalVariants,
    int InStock,
    int LowStock,
    int OutOfStock,
    List<LowStockProduct> LowStockProducts
);

public sealed record LowStockProduct(
    int VariantId,
    string ProductName,
    string VariantName,
    int StockQuantity,
    string Sku
);
