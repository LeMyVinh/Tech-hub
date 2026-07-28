using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class OrderStatusLog
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public int ChangedBy { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
