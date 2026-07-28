namespace ECommerce.Application;

public sealed record RegisterRequest(string? FullName, string? Email, string? Password, string? Phone);
public sealed record RegisterResponse(int Id, string FullName, string Email);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record RefreshRequest(string? RefreshToken);
public sealed record UserSummary(int Id, string FullName, string Role);
public sealed record LoginResponse(string Token, string RefreshToken, UserSummary User);
public sealed record ForgotPasswordRequest(string? Email);
public sealed record ResetPasswordRequest(string? Token, string? NewPassword);
public sealed record ChangePasswordRequest(string? OldPassword, string? NewPassword);

public sealed class AuthException : Exception
{
    public AuthException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}
