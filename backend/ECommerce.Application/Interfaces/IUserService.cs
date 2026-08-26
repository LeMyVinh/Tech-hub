namespace ECommerce.Application;

public interface IUserService
{
    Task<UserProfileResponse> GetUserProfileAsync(int userId);
    Task<UserProfileResponse> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request);
    Task<List<AddressResponse>> GetUserAddressesAsync(int userId);
    Task<AddressResponse> AddAddressAsync(int userId, AddAddressRequest request);
    Task<AddressResponse> UpdateAddressAsync(int userId, int addressId, UpdateAddressRequest request);
    Task DeleteAddressAsync(int userId, int addressId);
    Task SetDefaultAddressAsync(int userId, int addressId);
    Task<UserListResponse> GetAllUsersAsync(int page, int pageSize);
    Task LockUserAsync(int targetUserId);
    Task UnlockUserAsync(int targetUserId);
    Task SoftDeleteUserAsync(int targetUserId);
    Task RestoreUserAsync(int targetUserId);
}