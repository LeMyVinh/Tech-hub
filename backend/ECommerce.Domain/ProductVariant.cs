using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string VariantName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;
}
