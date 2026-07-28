namespace ECommerce.Application;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request);
    Task<string> DeleteAsync(int id);
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false);
    Task<ProductDetailResponse> GetDetailAsync(int id, bool includeInactive = false);
}
