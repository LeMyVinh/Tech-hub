namespace ECommerce.Application;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
    Task<OrderListResponse> GetUserOrdersAsync(int userId, int page, int pageSize);
    Task<OrderDetailResponse> GetOrderDetailAsync(int userId, int orderId);
    Task<OrderResponse> CancelOrderAsync(int userId, int orderId, string? reason);
    Task<OrderListResponse> GetAllOrdersAsync(int page, int pageSize, string? status);
    Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
}
