using ECommerce.Domain;

namespace ECommerce.Application;

public sealed class ProductService : IProductService
{
    private const int DetailReviewPageSize = 20;

    private static readonly HashSet<string> ValidProductStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Active", "Inactive" };

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly IReviewRepository _reviewRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IProductVariantRepository variantRepository,
        IProductImageRepository imageRepository,
        IReviewRepository reviewRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _variantRepository = variantRepository;
        _imageRepository = imageRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Vui lòng nhập tên sản phẩm.");

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null || category.IsActive != true)
            throw new CatalogException(400, "Danh mục không tồn tại hoặc đã bị ẩn.");

        var brand = await _brandRepository.GetByIdAsync(request.BrandId);
        if (brand == null || brand.IsActive != true)
            throw new CatalogException(400, "Thương hiệu không tồn tại hoặc đã bị ẩn.");

        if (request.Variants == null || request.Variants.Count == 0)
            throw new CatalogException(400, "Sản phẩm phải có ít nhất một biến thể.");

        // FIX (bug report #3): trước đây chỉ kiểm tra SKU trùng với DB
        // (ExistsBySkuAsync), không phát hiện được trường hợp 2 dòng biến thể MỚI
        // trong CÙNG một request có SKU trùng nhau -> DbUpdateException (unique
        // index) văng thẳng ra ngoài thành lỗi 500 thô cho Admin. Kiểm tra trùng
        // lặp ngay trong payload trước khi chạm DB.
        var duplicateSkuInRequest = request.Variants
            .Select(v => v.Sku?.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .GroupBy(s => s)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateSkuInRequest != null)
            throw new CatalogException(400, $"Mã SKU '{duplicateSkuInRequest.Key}' bị trùng lặp trong danh sách biến thể.");

        foreach (var v in request.Variants)
        {
            if (string.IsNullOrWhiteSpace(v.VariantName))
                throw new CatalogException(400, "Tên biến thể không được để trống.");
            if (string.IsNullOrWhiteSpace(v.Sku))
                throw new CatalogException(400, "Mã SKU không được để trống.");
            if (v.Price < 0)
                throw new CatalogException(400, "Giá sản phẩm phải lớn hơn hoặc bằng 0.");
            if (v.StockQuantity < 0)
                throw new CatalogException(400, "Số lượng tồn kho phải lớn hơn hoặc bằng 0.");

            var skuTrimmed = v.Sku.Trim();
            if (await _productRepository.ExistsBySkuAsync(skuTrimmed))
                throw new CatalogException(400, "Mã SKU đã tồn tại trong hệ thống.");
        }

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
        if (!ValidProductStatuses.Contains(status))
            throw new CatalogException(400, "Trạng thái sản phẩm không hợp lệ. Chỉ chấp nhận 'Active' hoặc 'Inactive'.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        var variantEntities = request.Variants.Select(v => new ProductVariant
        {
            ProductId = product.Id,
            VariantName = v.VariantName.Trim(),
            Sku = v.Sku.Trim(),
            Price = v.Price,
            StockQuantity = v.StockQuantity,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _variantRepository.AddRangeAsync(variantEntities);

        if (request.Images != null && request.Images.Count > 0)
        {
            var imageEntities = request.Images.Select(img => new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = img.ImageUrl.Trim(),
                IsPrimary = img.IsPrimary
            }).ToList();

            await _imageRepository.AddRangeAsync(imageEntities);
        }

        await _productRepository.SaveChangesAsync();

        var createdProduct = await _productRepository.GetWithDetailsAsync(product.Id, includeInactive: true);
        return MapToProductResponse(createdProduct!);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await _productRepository.GetWithDetailsAsync(id, includeInactive: true);
        if (product == null)
            throw new CatalogException(404, "Sản phẩm không tồn tại.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CatalogException(400, "Vui lòng nhập tên sản phẩm.");

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null || category.IsActive != true)
            throw new CatalogException(400, "Danh mục không tồn tại hoặc đã bị ẩn.");

        var brand = await _brandRepository.GetByIdAsync(request.BrandId);
        if (brand == null || brand.IsActive != true)
            throw new CatalogException(400, "Thương hiệu không tồn tại hoặc đã bị ẩn.");

        if (request.Variants == null || request.Variants.Count == 0)
            throw new CatalogException(400, "Sản phẩm phải có ít nhất một biến thể.");

        // FIX (bug report #3): cùng lý do như CreateAsync — chặn SKU trùng lặp
        // NGAY TRONG payload (bao gồm cả các dòng chỉnh sửa lẫn dòng mới thêm)
        // trước khi kiểm tra với DB, để trả lỗi 400 rõ ràng thay vì 500 thô.
        var duplicateSkuInRequest = request.Variants
            .Select(v => v.Sku?.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .GroupBy(s => s)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateSkuInRequest != null)
            throw new CatalogException(400, $"Mã SKU '{duplicateSkuInRequest.Key}' bị trùng lặp trong danh sách biến thể.");

        foreach (var v in request.Variants)
        {
            if (string.IsNullOrWhiteSpace(v.VariantName))
                throw new CatalogException(400, "Tên biến thể không được để trống.");
            if (string.IsNullOrWhiteSpace(v.Sku))
                throw new CatalogException(400, "Mã SKU không được để trống.");
            if (v.Price < 0)
                throw new CatalogException(400, "Giá sản phẩm phải lớn hơn hoặc bằng 0.");
            if (v.StockQuantity < 0)
                throw new CatalogException(400, "Số lượng tồn kho phải lớn hơn hoặc bằng 0.");

            var skuTrimmed = v.Sku.Trim();
            if (await _productRepository.ExistsBySkuAsync(skuTrimmed, excludeVariantId: v.Id))
                throw new CatalogException(400, "Mã SKU đã tồn tại trong hệ thống.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            if (!ValidProductStatuses.Contains(status))
                throw new CatalogException(400, "Trạng thái sản phẩm không hợp lệ. Chỉ chấp nhận 'Active' hoặc 'Inactive'.");
            product.Status = status;
        }

        var existingVariants = product.ProductVariants.ToList();
        var requestVariantIds = request.Variants.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();

        var candidatesToRemove = existingVariants.Where(v => !requestVariantIds.Contains(v.Id)).ToList();
        var toRemoveVariants = new List<ProductVariant>();
        foreach (var variant in candidatesToRemove)
        {
            var hasOrders = await _variantRepository.HasOrdersAsync(variant.Id);
            var hasCartItems = await _variantRepository.HasCartItemsAsync(variant.Id);

            if (hasOrders || hasCartItems)
            {
                variant.StockQuantity = 0;
                await _variantRepository.UpdateAsync(variant);
            }
            else
            {
                toRemoveVariants.Add(variant);
            }
        }

        if (toRemoveVariants.Count > 0)
        {
            await _variantRepository.DeleteRangeAsync(toRemoveVariants);
        }

        foreach (var vDto in request.Variants)
        {
            if (vDto.Id.HasValue)
            {
                var existing = existingVariants.FirstOrDefault(x => x.Id == vDto.Id.Value);
                if (existing != null)
                {
                    existing.VariantName = vDto.VariantName.Trim();
                    existing.Sku = vDto.Sku.Trim();
                    existing.Price = vDto.Price;
                    existing.StockQuantity = vDto.StockQuantity;
                    await _variantRepository.UpdateAsync(existing);
                }
            }
            else
            {
                var newVar = new ProductVariant
                {
                    ProductId = product.Id,
                    VariantName = vDto.VariantName.Trim(),
                    Sku = vDto.Sku.Trim(),
                    Price = vDto.Price,
                    StockQuantity = vDto.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                };
                await _variantRepository.AddAsync(newVar);
            }
        }

        var existingImages = product.ProductImages.ToList();
        var requestImageIds = (request.Images ?? new List<UpdateProductImageDto>())
            .Where(img => img.Id.HasValue).Select(img => img.Id!.Value).ToHashSet();

        var toRemoveImages = existingImages.Where(img => !requestImageIds.Contains(img.Id)).ToList();
        if (toRemoveImages.Count > 0)
        {
            await _imageRepository.DeleteRangeAsync(toRemoveImages);
        }

        if (request.Images != null)
        {
            foreach (var imgDto in request.Images)
            {
                if (imgDto.Id.HasValue)
                {
                    var existingImg = existingImages.FirstOrDefault(x => x.Id == imgDto.Id.Value);
                    if (existingImg != null)
                    {
                        existingImg.ImageUrl = imgDto.ImageUrl.Trim();
                        existingImg.IsPrimary = imgDto.IsPrimary;
                        await _imageRepository.UpdateAsync(existingImg);
                    }
                }
                else
                {
                    var newImg = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = imgDto.ImageUrl.Trim(),
                        IsPrimary = imgDto.IsPrimary
                    };
                    await _imageRepository.AddAsync(newImg);
                }
            }
        }

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        var updatedProduct = await _productRepository.GetWithDetailsAsync(id, includeInactive: true);
        return MapToProductResponse(updatedProduct!);
    }

    public async Task<string> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id, includeInactive: true);
        if (product == null)
            throw new CatalogException(404, "Sản phẩm không tồn tại.");

        var hasOrders = await _productRepository.HasOrdersAsync(id);
        product.Status = "Inactive";
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        if (hasOrders)
        {
            return "Không thể xoá sản phẩm đã phát sinh đơn hàng, chỉ có thể ẩn.";
        }

        return "Đã chuyển sản phẩm sang trạng thái ẩn.";
    }

    public async Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false)
    {
        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
            throw new CatalogException(400, "Khoảng giá không hợp lệ.");

        return await _productRepository.SearchAsync(filter, includeInactive);
    }

    public async Task<ProductDetailResponse> GetDetailAsync(int id, bool includeInactive = false)
    {
        var product = await _productRepository.GetWithDetailsAsync(id, includeInactive);
        if (product == null)
            throw new CatalogException(404, "Sản phẩm không tồn tại.");

        if (!includeInactive && product.Status != "Active")
            throw new CatalogException(400, "Sản phẩm hiện không còn kinh doanh.");

        var avgRating = await _reviewRepository.GetAverageRatingAsync(id);

        var totalReviewCount = await _reviewRepository.GetByProductIdCountAsync(id);
        var latestReviews = await _reviewRepository.GetByProductIdAsync(id, page: 1, pageSize: DetailReviewPageSize);

        var approvedReviews = latestReviews
            .Select(r => new ApprovedReviewSummaryResponse(
                r.Id,
                r.User.FullName,
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .ToList();

        return new ProductDetailResponse(
            product.Id,
            product.Name,
            product.Description,
            product.CategoryId,
            product.Category.Name,
            product.BrandId,
            product.Brand.Name,
            product.Status,
            product.ProductVariants.Select(v => new ProductVariantResponse(v.Id, v.VariantName, v.Sku, v.Price, v.StockQuantity)).ToList(),
            product.ProductImages.Select(img => new ProductImageResponse(img.Id, img.ImageUrl, img.IsPrimary)).ToList(),
            Math.Round(avgRating, 1),
            totalReviewCount,
            approvedReviews
        );
    }

    private static ProductResponse MapToProductResponse(Product p)
    {
        return new ProductResponse(
            p.Id,
            p.Name,
            p.Description,
            p.CategoryId,
            p.Category.Name,
            p.BrandId,
            p.Brand.Name,
            p.Status,
            p.ProductVariants.Select(v => new ProductVariantResponse(v.Id, v.VariantName, v.Sku, v.Price, v.StockQuantity)).ToList(),
            p.ProductImages.Select(img => new ProductImageResponse(img.Id, img.ImageUrl, img.IsPrimary)).ToList(),
            p.CreatedAt
        );
    }
}