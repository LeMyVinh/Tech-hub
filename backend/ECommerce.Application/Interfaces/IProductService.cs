namespace ECommerce.Application;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request);
    Task<string> DeleteAsync(int id);
    Task<string> RestoreAsync(int id);
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false, bool includeDeleted = false);
    Task<ProductDetailResponse> GetDetailAsync(int id, bool includeInactive = false, bool includeDeleted = false);
}
