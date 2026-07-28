using ECommerce.Domain;

namespace ECommerce.Application;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByIdWithDetailsAsync(int id);
    Task<List<Order>> GetUserOrdersAsync(int userId, int page, int pageSize);
    Task<int> GetUserOrdersCountAsync(int userId);
    Task<List<Order>> GetAllOrdersAsync(int page, int pageSize, string? status);
    Task<int> GetAllOrdersCountAsync(string? status);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
