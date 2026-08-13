using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    private const string CategoryTreeCacheKey = "product-repo:category-tree";
    private static readonly TimeSpan CategoryTreeCacheDuration = TimeSpan.FromMinutes(5);

    public ProductRepository(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Product?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var query = _db.Products.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(p => p.Status == "Active");
        }
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetWithDetailsAsync(int id, bool includeInactive = false)
    {
        // PERF FIX: trước đây Include(p => p.Reviews).ThenInclude(r => r.User) load
        // TOÀN BỘ review của sản phẩm (không giới hạn số lượng) mỗi khi khách xem
        // trang chi tiết, chỉ để tính điểm trung bình và hiển thị vài review mới
        // nhất. Với sản phẩm bán chạy có hàng nghìn review, việc này kéo tụt hiệu
        // năng nghiêm trọng. Giờ không include Reviews ở đây nữa — ProductService
        // lấy điểm trung bình + danh sách review (đã phân trang) qua
        // IReviewRepository (GetAverageRatingAsync / GetByProductIdAsync).
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p =>
                p.Status == "Active" &&
                p.Category.IsActive == true &&
                p.Brand.IsActive == true);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductFilterParams filter, bool includeInactive = false)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.Status == "Active" && p.Category.IsActive == true && p.Brand.IsActive == true);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw));
        }

        if (filter.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsWithDescendantsAsync(filter.CategoryId.Value);
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == filter.BrandId.Value);
        }
        if (filter.MinPrice.HasValue || filter.MaxPrice.HasValue)
        {
            var minPrice = filter.MinPrice;
            var maxPrice = filter.MaxPrice;
            query = query.Where(p => p.ProductVariants.Any(v =>
                (!minPrice.HasValue || v.Price >= minPrice.Value) &&
                (!maxPrice.HasValue || v.Price <= maxPrice.Value)));
        }

        query = filter.Sort?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.ProductVariants.Min(v => (decimal?)v.Price) ?? 0),
            "price_desc" => query.OrderByDescending(p => p.ProductVariants.Min(v => (decimal?)v.Price) ?? 0),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize > 100 ? 100 : (filter.PageSize < 1 ? 20 : filter.PageSize);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = products.Select(p =>
        {
            var prices = p.ProductVariants.Select(v => v.Price).ToList();
            var minPrice = prices.Count > 0 ? prices.Min() : 0;
            var maxPrice = prices.Count > 0 ? prices.Max() : 0;
            var primaryImage = p.ProductImages.FirstOrDefault(img => img.IsPrimary == true)?.ImageUrl
                ?? p.ProductImages.FirstOrDefault()?.ImageUrl;

            return new ProductSummaryResponse(
                p.Id,
                p.Name,
                p.Category.Name,
                p.Brand.Name,
                minPrice,
                maxPrice,
                primaryImage,
                p.Status
            );
        }).ToList();

        return new PagedResult<ProductSummaryResponse>(items, totalCount, page, pageSize);
    }

    private async Task<List<int>> GetCategoryIdsWithDescendantsAsync(int categoryId)
    {
        var childrenLookup = await GetCategoryChildrenLookupAsync();

        var result = new List<int> { categoryId };
        var queue = new Queue<int>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var childId in childrenLookup[current])
            {
                if (!result.Contains(childId))
                {
                    result.Add(childId);
                    queue.Enqueue(childId);
                }
            }
        }

        return result;
    }

    private async Task<ILookup<int, int>> GetCategoryChildrenLookupAsync()
    {
        if (_cache.TryGetValue<ILookup<int, int>>(CategoryTreeCacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var allCategories = await _db.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync();

        var lookup = allCategories
            .Where(c => c.ParentId.HasValue)
            .ToLookup(c => c.ParentId!.Value, c => c.Id);

        _cache.Set(CategoryTreeCacheKey, lookup, CategoryTreeCacheDuration);

        return lookup;
    }

    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeVariantId = null)
    {
        var lowerSku = sku.Trim().ToLower();
        return await _db.ProductVariants.AnyAsync(v =>
            v.Sku.ToLower() == lowerSku &&
            (excludeVariantId == null || v.Id != excludeVariantId));
    }

    public async Task<bool> HasOrdersAsync(int productId)
    {
        return await _db.OrderItems.AnyAsync(oi => oi.ProductVariant.ProductId == productId);
    }

    public async Task AddAsync(Product product)
    {
        await _db.Products.AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product)
    {
        _db.Products.Remove(product);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}