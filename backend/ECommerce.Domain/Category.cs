using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? ParentId { get; set; }

    public bool? IsActive { get; set; }

    // SOFT DELETE: Category có FK từ Product và self-FK (Parent). Service sẽ chặn
    // xóa nếu còn Product Active chưa xóa, nên soft delete là đủ. Cho phép khôi phục.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual Category? Parent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
