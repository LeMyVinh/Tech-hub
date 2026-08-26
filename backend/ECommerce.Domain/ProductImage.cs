using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    // SOFT DELETE: ProductImage có FK ProductId (ON DELETE CASCADE ở DB). Soft delete
    // cho phép admin "xóa" ảnh mà vẫn khôi phục được. Khi Product cha bị xóa mềm,
    // ảnh cũng nên ẩn theo (cascaded ở tầng service).
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
