using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Review
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public sbyte Rating { get; set; }

    public string? Comment { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    // SOFT DELETE: Review có FK từ OrderItem (UNIQUE). Soft delete giữ tham chiếu
    // cho OrderItem lịch sử nhưng ẩn review khỏi danh sách hiển thị của sản phẩm.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();

    public virtual User User { get; set; } = null!;
}
