using ECommerce.Domain;

namespace ECommerce.Application;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<Address>> GetByUserIdAsync(int userId, bool includeDeleted = false);
    Task AddAsync(Address address);
    Task UpdateAsync(Address address);
    Task SoftDeleteAsync(Address address);
    Task RestoreAsync(Address address);

    Task SaveChangesAsync();
}
