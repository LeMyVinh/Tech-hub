using ECommerce.Domain;

namespace ECommerce.Application;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task AddAsync(Payment payment);
    Task SaveChangesAsync();
}
