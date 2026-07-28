namespace ECommerce.Application;

public sealed record CreateProductVariantDto(
    string VariantName,
    string Sku,
    decimal Price,
    int StockQuantity
);

public sealed record UpdateProductVariantDto(
    int? Id,
    string VariantName,
    string Sku,
    decimal Price,
    int StockQuantity
);

public sealed record ProductVariantResponse(
    int Id,
    string VariantName,
    string Sku,
    decimal Price,
    int StockQuantity
);

public sealed record CreateProductImageDto(
    string ImageUrl,
    bool IsPrimary
);

public sealed record UpdateProductImageDto(
    int? Id,
    string ImageUrl,
    bool IsPrimary
);

public sealed record ProductImageResponse(
    int Id,
    string ImageUrl,
    bool IsPrimary
);

public sealed record CreateProductRequest(
    string? Name,
    string? Description,
    int CategoryId,
    int BrandId,
    List<CreateProductVariantDto>? Variants,
    List<CreateProductImageDto>? Images,
    string? Status
);

public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    int CategoryId,
    int BrandId,
    List<UpdateProductVariantDto>? Variants,
    List<UpdateProductImageDto>? Images,
    string? Status
);

public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    int BrandId,
    string BrandName,
    string Status,
    List<ProductVariantResponse> Variants,
    List<ProductImageResponse> Images,
    DateTime CreatedAt
);

public sealed record ProductFilterParams(
    string? Keyword = null,
    int? CategoryId = null,
    int? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20
);

public sealed record ProductSummaryResponse(
    int Id,
    string Name,
    string CategoryName,
    string BrandName,
    decimal MinPrice,
    decimal MaxPrice,
    string? PrimaryImageUrl,
    string Status
);

public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record ProductDetailResponse(
    int Id,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    int BrandId,
    string BrandName,
    string Status,
    List<ProductVariantResponse> Variants,
    List<ProductImageResponse> Images,
    double AvgRating,
    List<ApprovedReviewSummaryResponse> Reviews
);

public sealed record ApprovedReviewSummaryResponse(
    int Id,
    string UserName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
