namespace ECommerce.Application;

public interface IBrandService
{
    Task<BrandResponse> CreateAsync(CreateBrandRequest request);
    Task<BrandResponse> UpdateAsync(int id, UpdateBrandRequest request);
    Task<string> DeleteAsync(int id);
    Task<IEnumerable<BrandResponse>> GetAllAsync(bool includeInactive = false);
    Task<BrandResponse?> GetByIdAsync(int id);
}
