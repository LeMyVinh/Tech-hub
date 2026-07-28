namespace ECommerce.Application;

public interface ICategoryService
{
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);
    Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<string> DeleteAsync(int id);
    Task<IEnumerable<CategoryResponse>> GetAllAsync(bool includeInactive = false);
    Task<CategoryResponse?> GetByIdAsync(int id);
}
