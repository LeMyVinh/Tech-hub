using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int BrandId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // SOFT DELETE: Product có FK từ Review, WishlistItem, OrderItem. Soft delete giữ
    // tham chiếu từ các đơn hàng cũ + cho phép khôi phục. Query Filter sẽ tự ẩn
    // khỏi catalog và search.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
