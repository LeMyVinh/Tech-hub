using ECommerce.Domain;

namespace ECommerce.Application;

public sealed class BrandService : IBrandService
{
    private readonly IBrandRepository _brandRepository;

    public BrandService(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<BrandResponse> CreateAsync(CreateBrandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Tên thương hiệu không được để trống.");

        var trimmedName = request.Name.Trim();

        if (await _brandRepository.ExistsByNameAsync(trimmedName))
            throw new CatalogException(400, "Tên danh mục/thương hiệu đã tồn tại.");

        var brand = new Brand
        {
            Name = trimmedName,
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
        };

        await _brandRepository.AddAsync(brand);
        await _brandRepository.SaveChangesAsync();

        return MapToResponse(brand);
    }

    public async Task<BrandResponse> UpdateAsync(int id, UpdateBrandRequest request)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null)
            throw new CatalogException(404, "Thương hiệu không tồn tại.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Tên thương hiệu không được để trống.");

        var trimmedName = request.Name.Trim();

        if (await _brandRepository.ExistsByNameAsync(trimmedName, excludeId: id))
            throw new CatalogException(400, "Tên danh mục/thương hiệu đã tồn tại.");

        brand.Name = trimmedName;
        brand.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();

        await _brandRepository.UpdateAsync(brand);
        await _brandRepository.SaveChangesAsync();

        return MapToResponse(brand);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null)
            throw new CatalogException(404, "Thương hiệu không tồn tại.");

        // SOFT DELETE: cho phép xóa kể cả khi còn sản phẩm — dữ liệu vẫn giữ nguyên
        // qua FK, thương hiệu chỉ bị ẩn khỏi catalog và admin có thể khôi phục sau.
        await _brandRepository.SoftDeleteAsync(brand);

        return "Đã xóa thương hiệu thành công.";
    }

    public async Task<string> RestoreAsync(int id)
    {
        var brand = await _brandRepository.GetByIdIncludingDeletedAsync(id);
        if (brand == null)
            throw new CatalogException(404, "Thương hiệu không tồn tại.");

        if (!brand.IsDeleted)
            return "Thương hiệu đang hoạt động.";

        await _brandRepository.RestoreAsync(brand);

        return "Đã khôi phục thương hiệu thành công.";
    }

    public async Task<IEnumerable<BrandResponse>> GetAllAsync(bool includeDeleted = false)
    {
        var brands = await _brandRepository.GetAllAsync(includeDeleted);
        return brands.Select(MapToResponse);
    }

    public async Task<BrandResponse?> GetByIdAsync(int id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null) return null;
        return MapToResponse(brand);
    }

    private static BrandResponse MapToResponse(Brand b)
    {
        return new BrandResponse(
            b.Id,
            b.Name,
            b.LogoUrl,
            b.IsDeleted,
            b.DeletedAt
        );
    }
}