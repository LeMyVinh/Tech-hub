using ECommerce.Domain;

namespace ECommerce.Application;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Tên danh mục không được để trống.");

        var trimmedName = request.Name.Trim();

        if (await _categoryRepository.ExistsByNameAsync(trimmedName, request.ParentId))
            throw new CatalogException(400, "Tên danh mục/thương hiệu đã tồn tại.");

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value);
            if (parent == null || parent.IsActive != true)
                throw new CatalogException(400, "Danh mục cha không tồn tại hoặc đã bị ẩn.");
        }

        var category = new Category
        {
            Name = trimmedName,
            ParentId = request.ParentId,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        var created = await _categoryRepository.GetByIdAsync(category.Id);
        return MapToResponse(created!);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new CatalogException(404, "Danh mục không tồn tại.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Tên danh mục không được để trống.");

        var trimmedName = request.Name.Trim();

        if (request.ParentId.HasValue && request.ParentId.Value == id)
            throw new CatalogException(400, "Danh mục cha không thể là chính nó.");

        if (await _categoryRepository.ExistsByNameAsync(trimmedName, request.ParentId, excludeId: id))
            throw new CatalogException(400, "Tên danh mục/thương hiệu đã tồn tại.");

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value);
            if (parent == null || parent.IsActive != true)
                throw new CatalogException(400, "Danh mục cha không tồn tại hoặc đã bị ẩn.");
        }

        category.Name = trimmedName;
        category.ParentId = request.ParentId;

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        var updated = await _categoryRepository.GetByIdAsync(id);
        return MapToResponse(updated!);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new CatalogException(404, "Danh mục không tồn tại.");

        var hasActiveProducts = await _categoryRepository.HasActiveProductsAsync(id);
        category.IsActive = false;

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        if (hasActiveProducts)
        {
            return "Không thể xoá, danh mục/thương hiệu đang có sản phẩm. Đã chuyển sang trạng thái ẩn.";
        }

        return "Đã ngừng sử dụng danh mục thành công.";
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync(bool includeInactive = false)
    {
        var categories = await _categoryRepository.GetAllAsync(includeInactive);
        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;
        return MapToResponse(category);
    }

    private static CategoryResponse MapToResponse(Category c)
    {
        return new CategoryResponse(
            c.Id,
            c.Name,
            c.ParentId,
            c.Parent?.Name,
            c.IsActive ?? false
        );
    }
}
