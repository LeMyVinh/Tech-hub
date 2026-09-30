using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;
[ApiController]
[Route("api/v1/auth")]
[Consumes("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]

    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (AuthException ex) { return Error(ex); }
        catch (DbUpdateException)
        {
            return StatusCode(StatusCodes.Status201Created,
                new RegisterResponse(0, request.FullName ?? string.Empty, request.Email?.Trim().ToLowerInvariant() ?? string.Empty));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try { return Ok(await _authService.LoginAsync(request)); }
        catch (AuthException ex) { return Error(ex); }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        try { return Ok(await _authService.RefreshAsync(request)); }
        catch (AuthException ex) { return Error(ex); }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await _authService.LogoutAsync(request);
        return Ok(new { message = "Đăng xuất thành công." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Email khôi phục đã được gửi." });
        }
        catch (AuthException ex) { return Error(ex); }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
        catch (AuthException ex) { return Error(ex); }
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
        catch (AuthException ex) { return Error(ex); }
    }

    // EMAIL VERIFICATION
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        try
        {
            await _authService.VerifyEmailAsync(request);
            return Ok(new { message = "Xác thực email thành công. Bạn có thể đăng nhập ngay bây giờ." });
        }
        catch (AuthException ex) { return Error(ex); }
    }

    // EMAIL VERIFICATION
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationEmailRequest request)
    {
        try
        {
            await _authService.ResendVerificationEmailAsync(request);
            return Ok(new { message = "Nếu email tồn tại và chưa xác thực, chúng tôi đã gửi lại liên kết xác thực." });
        }
        catch (AuthException ex) { return Error(ex); }
    }

    private ObjectResult Error(AuthException exception) => StatusCode(exception.StatusCode, new { message = exception.Message });
}