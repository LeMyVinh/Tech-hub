using ECommerce.Application;
using ECommerce.Domain;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
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
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.Status == "Active");
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
            // CAT-029 fix (BR-09): "Sản phẩm ngừng kinh doanh HOẶC hết hàng không
            // hiển thị trong kết quả tìm kiếm mặc định." Trước đây chỉ lọc theo
            // Status/Category/Brand, sản phẩm hết hàng (mọi variant StockQuantity=0)
            // vẫn lọt vào danh sách công khai. Chỉ áp dụng cho search PUBLIC
            // (includeInactive=false) - Admin (AdminProductController dùng
            // includeInactive=true) vẫn phải thấy đầy đủ để còn quản lý tồn kho.
            query = query.Where(p =>
                p.Status == "Active" &&
                p.Category.IsActive == true &&
                p.Brand.IsActive == true &&
                p.ProductVariants.Any(v => v.StockQuantity > 0));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw));
        }

        if (filter.CategoryId.HasValue)
        {
            // Lọc theo danh mục sẽ bao gồm cả các danh mục con (vd: bấm "Laptop" thì cũng
            // ra sản phẩm thuộc "Laptop MacBook", "Laptop Windows" là con của "Laptop")
            var categoryIds = await GetCategoryIdsWithDescendantsAsync(filter.CategoryId.Value);
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (filter.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == filter.BrandId.Value);
        }

        // CAT-015 fix: minPrice và maxPrice trước đây được áp dụng bằng 2 mệnh đề
        // Any() ĐỘC LẬP, nên 1 sản phẩm có variant A (giá thấp, thoả maxPrice) và
        // variant B (giá cao, thoả minPrice) vẫn lọt qua filter dù KHÔNG variant nào
        // thực sự nằm trong khoảng [minPrice, maxPrice]. Gộp lại thành 1 Any() để cả
        // 2 điều kiện luôn được xét trên CÙNG một variant.
        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue)
        {
            var min = filter.MinPrice.Value;
            var max = filter.MaxPrice.Value;
            query = query.Where(p => p.ProductVariants.Any(v => v.Price >= min && v.Price <= max));
        }
        else if (filter.MinPrice.HasValue)
        {
            var min = filter.MinPrice.Value;
            query = query.Where(p => p.ProductVariants.Any(v => v.Price >= min));
        }
        else if (filter.MaxPrice.HasValue)
        {
            var max = filter.MaxPrice.Value;
            query = query.Where(p => p.ProductVariants.Any(v => v.Price <= max));
        }

        query = filter.Sort?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.ProductVariants.Min(v => (decimal?)v.Price) ?? 0),
            "price_desc" => query.OrderByDescending(p => p.ProductVariants.Min(v => (decimal?)v.Price) ?? 0),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            // CAT-019 fix: DD liệt kê "best_selling" là 1 tuỳ chọn sort hợp lệ nhưng
            // trước đây KHÔNG được cài đặt (rơi vào nhánh mặc định OrderByDescending(Id),
            // tức không hề sắp theo số lượng bán). Tổng số lượng đã bán = tổng Quantity
            // của mọi OrderItem thuộc mọi variant của sản phẩm.
            "best_selling" => query.OrderByDescending(p =>
                p.ProductVariants.SelectMany(v => v.OrderItems).Sum(oi => (int?)oi.Quantity) ?? 0),
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

    /// <summary>
    /// Trả về danh sách gồm chính categoryId truyền vào và toàn bộ ID các danh mục con
    /// (đệ quy nhiều cấp), dùng để lọc sản phẩm theo cả cây danh mục thay vì chỉ 1 ID duy nhất.
    /// </summary>
    private async Task<List<int>> GetCategoryIdsWithDescendantsAsync(int categoryId)
    {
        var allCategories = await _db.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync();

        var result = new List<int> { categoryId };
        var queue = new Queue<int>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = allCategories.Where(c => c.ParentId == current).Select(c => c.Id);

            foreach (var childId in children)
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