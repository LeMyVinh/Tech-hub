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

        // FIX: chặn vòng lặp nhiều cấp (A -> B -> C -> A). Trước đây chỉ kiểm tra
        // ParentId trùng chính Id (vòng lặp 1 cấp), không phát hiện được trường hợp
        // gán một danh mục CON/CHÁU của chính nó làm cha, khiến cây danh mục bị đứt
        // gãy logic phân cấp (breadcrumb, lọc sản phẩm theo cây category sẽ sai).
        if (request.ParentId.HasValue &&
            await IsDescendantOfAsync(request.ParentId.Value, id))
        {
            throw new CatalogException(400,
                "Không thể chọn danh mục con của chính nó làm danh mục cha (sẽ tạo vòng lặp).");
        }

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

        // FIX: trước đây category.IsActive luôn bị set false ngay cả khi đang có sản
        // phẩm Active gắn với nó, khiếm toàn bộ sản phẩm đó "biến mất" âm thầm khỏi
        // trang khách hàng (ProductRepository.SearchAsync lọc theo Category.IsActive).
        // Giờ chặn hẳn thao tác ẩn nếu còn sản phẩm đang kinh doanh, buộc Admin phải
        // chuyển sản phẩm sang danh mục khác trước.
        var hasActiveProducts = await _categoryRepository.HasActiveProductsAsync(id);
        if (hasActiveProducts)
        {
            throw new CatalogException(400,
                "Không thể ẩn danh mục đang có sản phẩm đang kinh doanh. Vui lòng chuyển sản phẩm sang danh mục khác trước khi ẩn.");
        }

        // SOFT DELETE: chuyển từ cơ chế ẩn bằng IsActive=false sang IsDeleted=true.
        // Global Query Filter sẽ tự loại khỏi mọi query (kể cả khi includeInactive).
        // Vẫn giữ IsActive=false để tương thích ngược với code cũ.
        await _categoryRepository.SoftDeleteAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return "Đã xóa danh mục thành công.";
    }

    public async Task<string> RestoreAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id, includeDeleted: true);
        if (category == null)
            throw new CatalogException(404, "Danh mục không tồn tại.");
        if (!category.IsDeleted)
            throw new CatalogException(400, "Danh mục này chưa bị xóa.");

        await _categoryRepository.RestoreAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return "Đã khôi phục danh mục thành công.";
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false)
    {
        var categories = await _categoryRepository.GetAllAsync(includeInactive, includeDeleted);
        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return null;
        return MapToResponse(category);
    }

    // FIX: đi ngược từ candidateParentId lên tới gốc (theo ParentId), nếu gặp lại
    // ancestorId (= category đang sửa) thì nghĩa là candidateParentId đang nằm trong
    // nhánh con/cháu của ancestorId -> gán làm cha sẽ tạo vòng lặp. `visited` chỉ để
    // phòng hờ dữ liệu cũ trong DB đã lỡ bị vòng lặp, tránh loop vô hạn.
    private async Task<bool> IsDescendantOfAsync(int candidateParentId, int ancestorId)
    {
        var allCategories = await _categoryRepository.GetAllAsync(includeInactive: true);
        var byId = allCategories.ToDictionary(c => c.Id);

        var current = candidateParentId;
        var visited = new HashSet<int>();

        while (byId.TryGetValue(current, out var node))
        {
            if (node.Id == ancestorId) return true;
            if (!node.ParentId.HasValue) break;
            if (!visited.Add(node.Id)) break;
            current = node.ParentId.Value;
        }

        return false;
    }

    private static CategoryResponse MapToResponse(Category c)
    {
        return new CategoryResponse(
            c.Id,
            c.Name,
            c.ParentId,
            c.Parent?.Name,
            c.IsActive ?? false,
            c.IsDeleted,
            c.DeletedAt
        );
    }
}
