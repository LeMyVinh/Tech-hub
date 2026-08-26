namespace ECommerce.Application;

public sealed record UserProfileResponse(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt
);

public sealed record UpdateUserProfileRequest(
    string? FullName,
    string? Phone
);

public sealed record AddAddressRequest(
    string RecipientName,
    string Phone,
    string DetailAddress,
    string Ward,
    string District,
    string Province,
    bool IsDefault
);

public sealed record UpdateAddressRequest(
    string RecipientName,
    string Phone,
    string DetailAddress,
    string Ward,
    string District,
    string Province,
    bool IsDefault
);

public sealed record UserListResponse(
    List<UserProfileResponse> Users,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed class UserException : Exception
{
    public UserException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
