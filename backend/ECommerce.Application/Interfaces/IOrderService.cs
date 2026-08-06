namespace ECommerce.Application;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request, string clientIp, string returnUrl);
    Task<OrderListResponse> GetUserOrdersAsync(int userId, int page, int pageSize);
    Task<OrderDetailResponse> GetOrderDetailAsync(int userId, int orderId, bool isAdmin = false);
    Task<OrderResponse> CancelOrderAsync(int userId, int orderId, string? reason);
    Task<OrderListResponse> GetAllOrdersAsync(int page, int pageSize, string? status);
    Task<OrderResponse> UpdateOrderStatusAsync(int adminUserId, int orderId, UpdateOrderStatusRequest request);
}