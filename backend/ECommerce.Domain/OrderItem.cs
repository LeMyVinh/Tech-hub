using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual Review? Review { get; set; }
}
