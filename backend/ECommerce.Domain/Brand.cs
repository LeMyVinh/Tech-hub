using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
