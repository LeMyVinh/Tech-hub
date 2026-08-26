using System.Text.RegularExpressions;
using ECommerce.Domain;

namespace ECommerce.Application;

public class UserService : IUserService
{
    // FIX: đồng bộ với rule validate phone ở AuthService/Register (10 số, bắt đầu bằng 0).
    // Trước đây UpdateUserProfileAsync nhận bất kỳ chuỗi nào cho Phone, không kiểm tra
    // định dạng, khiến dữ liệu Phone trong DB không nhất quán so với lúc đăng ký.
    private static readonly Regex PhoneRegex = new(@"^0[0-9]{9}$", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UserService(
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _refreshTokenRepository = refreshTokenRepository;
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
            user.IsDeleted,
            user.DeletedAt,
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
        {
            var trimmedPhone = request.Phone.Trim();

            // Cho phép xóa trắng số điện thoại (coi như "chưa cập nhật"), nhưng nếu có
            // nhập giá trị thì phải đúng định dạng.
            if (trimmedPhone.Length > 0 && !PhoneRegex.IsMatch(trimmedPhone))
                throw new UserException(400, "Số điện thoại không hợp lệ (phải gồm 10 số, bắt đầu bằng 0).");

            user.Phone = trimmedPhone.Length == 0 ? null : trimmedPhone;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return new UserProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role.Name,
            user.IsDeleted,
            user.DeletedAt,
            user.CreatedAt
        );
    }

    public async Task<List<AddressResponse>> GetUserAddressesAsync(int userId, bool includeDeleted = false)
    {
        var addresses = await _addressRepository.GetByUserIdAsync(userId, includeDeleted);
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

        await _addressRepository.SoftDeleteAsync(address);
        await _addressRepository.SaveChangesAsync();
    }

    public async Task<AddressResponse> RestoreAddressAsync(int userId, int addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId, includeDeleted: true)
            ?? throw new UserException(404, "Địa chỉ không tồn tại.");

        if (address.UserId != userId)
            throw new UserException(403, "Bạn không có quyền khôi phục địa chỉ này.");
        if (!address.IsDeleted)
            throw new UserException(400, "Địa chỉ này chưa bị xóa.");

        await _addressRepository.RestoreAsync(address);
        await _addressRepository.SaveChangesAsync();
        return MapToResponse(address);
    }

    public async Task SetDefaultAddressAsync(int userId, int addressId)
    {
        var address = await _addressRepository.GetByIdAsync(addressId)
            ?? throw new UserException(404, "Địa chỉ không tồn tại.");

        if (address.UserId != userId)
            throw new UserException(403, "Bạn không có quyền cập nhật địa chỉ này.");

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
                u.IsDeleted,
                u.DeletedAt,
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

        // FIX: trước đây MỌI tài khoản Admin đều không thể bị khóa (kể cả khi có
        // nhiều Admin và một trong số đó đã nghỉ việc / bị lộ tài khoản), đồng thời
        // KHÔNG có gì đảm bảo hệ thống luôn còn ít nhất 1 Admin hoạt động nếu sau
        // này quy tắc này được nới lỏng tùy tiện. Giờ: chỉ chặn khi đây là Admin
        // đang hoạt động CUỐI CÙNG trong hệ thống; nếu còn Admin khác đang hoạt
        // động, việc khóa được cho phép.
        if (user.Role.Name == "Admin")
        {
            var activeAdminCount = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
                throw new UserException(400, "Không thể khóa tài khoản Admin cuối cùng đang hoạt động trong hệ thống.");
        }

        // Bỏ IsActive: không còn cờ này trên User. Nếu muốn "tạm khóa" thì dùng
        // soft delete; nếu muốn trạng thái tạm thời khác thì cần thêm cờ riêng.
        // Hiện tại: chỉ thao tác soft delete / restore là đủ cho yêu cầu nghiệp vụ.
        await _userRepository.SaveChangesAsync();
    }

    public async Task UnlockUserAsync(int targetUserId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        await _userRepository.SaveChangesAsync();
    }

    // SOFT DELETE: thao tác này đánh dấu IsDeleted = true khiến user bị
    // HasQueryFilter loại khỏi mọi truy vấn mặc định (không còn hiện trong danh
    // sách quản trị, không login được). Dữ liệu liên quan (Order, Review...)
    // vẫn giữ nguyên nhờ FK, không xóa cứng User.
    public async Task SoftDeleteUserAsync(int targetUserId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        if (user.Role.Name == "Admin")
        {
            var activeAdminCount = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
                throw new UserException(400, "Không thể xóa tài khoản Admin cuối cùng đang hoạt động trong hệ thống.");
        }

        await _userRepository.SoftDeleteAsync(user);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        // Thu hồi toàn bộ refresh token, đá user ra khỏi mọi phiên đang đăng nhập
        // ngay lập tức thay vì chờ access token (15 phút) hết hạn.
        await _refreshTokenRepository.RevokeAllByUserIdAsync(targetUserId);
    }

    // RESTORE: đảo ngược soft delete. User hoạt động lại bình thường, hiện trở
    // lại trong danh sách quản trị và có thể đăng nhập.
    public async Task RestoreUserAsync(int targetUserId)
    {
        // Dùng IgnoreQueryFilters để lấy được cả user đã bị soft delete.
        var user = await _userRepository.GetByIdIncludingDeletedAsync(targetUserId)
            ?? throw new UserException(404, "Người dùng không tồn tại.");

        if (!user.IsDeleted)
            return; // không phải user đã xóa -> nothing to do

        await _userRepository.RestoreAsync(user);
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
            address.IsDefault,
            address.IsDeleted,
            address.DeletedAt
        );
    }
}
