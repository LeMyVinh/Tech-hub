using System;
using System.Collections.Generic;

namespace ECommerce.Domain;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // AUTH-079 fix: brute-force lockout bookkeeping.
    // FailedLoginAttempts resets to 0 on every successful login;
    // LockedUntil is set once the attempt count hits the threshold and
    // cleared again once it naturally expires or login succeeds.
    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    // EMAIL VERIFICATION: false cho tới khi user bấm link xác thực gửi qua email.
    // Login bị chặn (403) khi cờ này còn false, xem AuthService.LoginAsync.
    public bool EmailVerified { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<OrderStatusLog> OrderStatusLogs { get; set; } = new List<OrderStatusLog>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}