namespace ECommerce.Application;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(DateTime? startDate, DateTime? endDate);
    Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? startDate, DateTime? endDate);
    Task<TopProductsResponse> GetTopProductsAsync(int limit, DateTime? startDate, DateTime? endDate);
    Task<InventoryReportResponse> GetInventoryReportAsync();
}
