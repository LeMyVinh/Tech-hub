namespace ECommerce.Application;

public interface IDashboardRepository
{
    Task<DashboardRawData> GetRawDataAsync(DateTime startDate, DateTime endDate);
    Task<List<ProductStockData>> GetProductStockDataAsync();
}

public sealed record DashboardRawData(
    decimal CurrentPeriodRevenue,
    decimal PreviousPeriodRevenue,
    int CurrentPeriodOrders,
    int PreviousPeriodOrders,
    int TotalCustomers,
    int TotalActiveProducts,
    int PendingOrders,
    List<OrderWithItems> OrdersWithItems
);

public sealed record OrderWithItems(
    DateTime CreatedAt,
    decimal TotalAmount,
    string Status,
    List<OrderItemData> Items
);

public sealed record OrderItemData(
    int ProductId,
    string ProductName,
    string CategoryName,
    int Quantity,
    decimal UnitPrice,
    string? ImageUrl
);

public sealed record ProductStockData(
    int VariantId,
    int ProductId,
    string ProductName,
    string VariantName,
    string Sku,
    int StockQuantity
);
