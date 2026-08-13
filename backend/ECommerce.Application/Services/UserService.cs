using ECommerce.Domain;

namespace ECommerce.Application;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;

    public UserService(IUserRepository userRepository, IAddressRepository addressRepository)
    {
        _userRepository = userRepository;
        _addressRepository = addressRepository;
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        return new UserProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role.Name,
            user.IsActive ?? true,
            user.CreatedAt
        );
    }

    public async Task<UserProfileResponse> UpdateUserProfileAsync(int userId, UpdateUserProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (request.Phone is not null)
            user.Phone = request.Phone.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return new UserProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role.Name,
            user.IsActive ?? true,
            user.CreatedAt
        );
    }

    public async Task<List<AddressResponse>> GetUserAddressesAsync(int userId)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId);
        return addresses.Select(MapToResponse).ToList();
    }

    public async Task<AddressResponse> AddAddressAsync(int userId, AddAddressRequest request)
    {
        var address = new Address
        {
            UserId = userId,
            RecipientName = request.RecipientName,
            Phone = request.Phone,
            DetailAddress = request.DetailAddress,
            Ward = request.Ward,
            District = request.District,
            Province = request.Province,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        // If setting as default, unset other defaults
        if (request.IsDefault)
        {
            var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
            foreach (var existing in existingAddresses.Where(a => a.IsDefault))
            {
                existing.IsDefault = false;
                await _addressRepository.UpdateAsync(existing);
            }
        }

        await _addressRepository.AddAsync(address);
        await _addressRepository.SaveChangesAsync();

        return MapToResponse(address);
    }

    public async Task<AddressResponse> UpdateAddressAsync(int userId, int addressId, UpdateAddressRequest request)
    {
        var address = await _addressRepository.GetByIdAsync(addressId)
            ?? throw new UserException(404, "Địa chỉ không tồn tại.");

        if (address.UserId != userId)
            throw new UserException(403, "Bạn không có quyền cập nhật địa chỉ này.");

        address.RecipientName = request.RecipientName;
        address.Phone = request.Phone;
        address.DetailAddress = request.DetailAddress;
        address.Ward = request.Ward;
        address.District = request.District;
        address.Province = request.Province;

        // If setting as default, unset other defaults
        if (request.IsDefault && !address.IsDefault)
        {
            var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
            foreach (var existing in existingAddresses.Where(a => a.IsDefault && a.Id != addressId))
            {
                existing.IsDefault = false;
                await _addressRepository.UpdateAsync(existing);
            }
        }

        address.IsDefault = request.IsDefault;

        await _addressRepository.UpdateAsync(address);
        await _addressRepository.SaveChangesAsync();

        return MapToResponse(address);
    }

    public async Task DeleteAddressAsync(int userId, int addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId)
            ?? throw new UserException(404, "Địa chỉ không tồn tại.");

        if (address.UserId != userId)
            throw new UserException(403, "Bạn không có quyền xóa địa chỉ này.");

        // FIX: chặn xóa địa chỉ đã gắn với đơn hàng, tránh vi phạm FK constraint
        // (Order.AddressId not-null) hoặc mất dữ liệu lịch sử đơn hàng.
        if (await _addressRepository.HasOrdersAsync(addressId))
            throw new UserException(400,
                "Không thể xóa địa chỉ đã được dùng để đặt hàng. Bạn có thể thêm địa chỉ mới thay thế.");

        await _addressRepository.DeleteAsync(address);
        await _addressRepository.SaveChangesAsync();
    }

    public async Task SetDefaultAddressAsync(int userId, int addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId)
            ?? throw new UserException(404, "Địa chỉ không tồn tại.");

        if (address.UserId != userId)
            throw new UserException(403, "Bạn không có quyền cập nhật địa chỉ này.");

        // Unset other defaults
        var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
        foreach (var existing in existingAddresses.Where(a => a.IsDefault))
        {
            existing.IsDefault = false;
            await _addressRepository.UpdateAsync(existing);
        }

        address.IsDefault = true;
        await _addressRepository.UpdateAsync(address);
        await _addressRepository.SaveChangesAsync();
    }

    public async Task<UserListResponse> GetAllUsersAsync(int page, int pageSize)
    {
        var users = await _userRepository.GetAllAsync(page, pageSize);
        var totalCount = await _userRepository.GetCountAsync();

        return new UserListResponse(
            users.Select(u => new UserProfileResponse(
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role.Name,
                u.IsActive ?? true,
                u.CreatedAt
            )).ToList(),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task LockUserAsync(int targetUserId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        if (user.Role.Name == "Admin")
            throw new UserException(400, "Không thể khóa tài khoản Admin.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    public async Task UnlockUserAsync(int targetUserId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
    }

    private static AddressResponse MapToResponse(Address address)
    {
        return new AddressResponse(
            address.Id,
            address.RecipientName,
            address.Phone,
            address.DetailAddress,
            address.Ward,
            address.District,
            address.Province,
            address.IsDefault
        );
    }
}