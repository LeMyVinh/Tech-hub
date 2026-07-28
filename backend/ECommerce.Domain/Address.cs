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

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
