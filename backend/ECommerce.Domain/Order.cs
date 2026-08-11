using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AddressId { get; set; }

    public string ShippingMethod { get; set; } = null!;

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusLog> OrderStatusLogs { get; set; } = new List<OrderStatusLog>();

    public virtual Payment? Payment { get; set; }

    public virtual User User { get; set; } = null!;
}