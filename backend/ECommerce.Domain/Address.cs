using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Address
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string RecipientName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string District { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string DetailAddress { get; set; } = null!;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    // SOFT DELETE: Address có FK từ Order (lịch sử đơn hàng) nên xóa cứng sẽ vi phạm
    // FK constraint hoặc cascade xóa mất đơn. Soft delete giữ địa chỉ cho lịch sử
    // nhưng ẩn khỏi danh sách địa chỉ hiện tại của user.
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
