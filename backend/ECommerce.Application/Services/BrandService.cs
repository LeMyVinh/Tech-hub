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
            IsActive = true
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

        var hasActiveProducts = await _brandRepository.HasActiveProductsAsync(id);
        brand.IsActive = false;

        await _brandRepository.UpdateAsync(brand);
        await _brandRepository.SaveChangesAsync();

        if (hasActiveProducts)
        {
            return "Không thể xoá, danh mục/thương hiệu đang có sản phẩm. Đã chuyển sang trạng thái ẩn.";
        }

        return "Đã ngừng sử dụng thương hiệu thành công.";
    }

    public async Task<IEnumerable<BrandResponse>> GetAllAsync(bool includeInactive = false)
    {
        var brands = await _brandRepository.GetAllAsync(includeInactive);
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
            b.IsActive ?? false
        );
    }
}
