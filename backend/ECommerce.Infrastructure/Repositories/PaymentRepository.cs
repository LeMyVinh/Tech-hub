using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly Data.AppDbContext _context;

    public PaymentRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
