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

        // FIX (TC-06): trước đây Brand cho xóa ngay cả khi còn sản phẩm đang kinh doanh
        // (Active) thuộc thương hiệu đó, trong khi Category đã có sẵn cơ chế chặn này
        // (HasActiveProductsAsync). Vì ProductRepository lọc sản phẩm hiển thị công khai
        // theo điều kiện "_db.Brands.Any(b => b.Id == p.BrandId)" (Brand có Global Query
        // Filter !IsDeleted), xóa mềm Brand khiến TOÀN BỘ sản phẩm thuộc brand đó biến mất
        // khỏi trang khách hàng NGAY LẬP TỨC dù bản thân Product.Status vẫn là "Active" --
        // và Admin không hề được cảnh báo gì, dễ hiểu nhầm là bug/mất dữ liệu. Đồng bộ hành
        // vi với CategoryService: chặn xóa nếu còn sản phẩm Active, buộc Admin chuyển sản
        // phẩm sang thương hiệu khác trước.
        var hasActiveProducts = await _brandRepository.HasActiveProductsAsync(id);
        if (hasActiveProducts)
        {
            throw new CatalogException(400,
                "Không thể xóa thương hiệu đang có sản phẩm đang kinh doanh. Vui lòng chuyển sản phẩm sang thương hiệu khác trước khi xóa.");
        }

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