using ECommerce.Domain;

namespace ECommerce.Application;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(int id);
    Task<List<Address>> GetByUserIdAsync(int userId);
    Task AddAsync(Address address);
    Task UpdateAsync(Address address);
    Task DeleteAsync(Address address);

    // FIX: Order.AddressId là FK not-null trỏ tới Address. Phải kiểm tra địa chỉ đã
    // từng được dùng để đặt hàng trước khi cho xóa, nếu không sẽ crash 500 (vi phạm
    // FK constraint) hoặc, tệ hơn, xóa cascade luôn đơn hàng lịch sử nếu DB có cấu
    // hình ON DELETE CASCADE trên FK này.
    Task<bool> HasOrdersAsync(int addressId);

    Task SaveChangesAsync();
}