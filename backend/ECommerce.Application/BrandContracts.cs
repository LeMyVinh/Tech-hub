namespace ECommerce.Application;

public sealed record CreateBrandRequest(string? Name, string? LogoUrl);

public sealed record UpdateBrandRequest(string? Name, string? LogoUrl);

public sealed record BrandResponse(
    int Id,
    string Name,
    string? LogoUrl,
    bool IsDeleted,
    DateTime? DeletedAt);
