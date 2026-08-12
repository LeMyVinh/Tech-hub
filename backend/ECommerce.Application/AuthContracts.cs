namespace ECommerce.Application;

public sealed record RegisterRequest(string? FullName, string? Email, string? Password, string? Phone);
public sealed record RegisterResponse(int Id, string FullName, string Email);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record RefreshRequest(string? RefreshToken);
public sealed record UserSummary(int Id, string FullName, string Role);
public sealed record LoginResponse(string Token, string RefreshToken, UserSummary User);
public sealed record ForgotPasswordRequest(string? Email);
public sealed record ResetPasswordRequest(string? Token, string? NewPassword);

// AUTH-099 fix: ConfirmNewPassword is now part of the server-side contract too,
// so mismatches are rejected by the API itself instead of relying only on the
// Angular form (which the API cannot trust, e.g. direct curl/Postman calls).
public sealed record ChangePasswordRequest(string? OldPassword, string? NewPassword, string? ConfirmNewPassword);

// EMAIL VERIFICATION
public sealed record VerifyEmailRequest(string? Token);
public sealed record ResendVerificationEmailRequest(string? Email);

public sealed class AuthException : Exception
{
    public AuthException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}