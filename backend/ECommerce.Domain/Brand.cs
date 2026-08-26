using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? LogoUrl { get; set; }

    // SOFT DELETE: chỉ dùng IsDeleted (giống User). Brand bị lọc qua HasQueryFilter
    // trong AppDbContext; admin dùng IgnoreQueryFilters khi cần xem/khôi phục.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
