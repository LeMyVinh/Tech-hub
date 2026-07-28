using ECommerce.Domain;

namespace ECommerce.Application;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(int id);
    Task<List<Address>> GetByUserIdAsync(int userId);
    Task AddAsync(Address address);
    Task UpdateAsync(Address address);
    Task DeleteAsync(Address address);
    Task SaveChangesAsync();
}
