using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class PasswordResetToken
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiredAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
