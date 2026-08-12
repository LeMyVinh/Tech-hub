namespace ECommerce.Application;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(RefreshRequest request);
    Task LogoutAsync(RefreshRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);

    // EMAIL VERIFICATION
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request);
}