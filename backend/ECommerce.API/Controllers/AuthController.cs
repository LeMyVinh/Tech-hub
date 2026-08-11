using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
// AUTH-108 fix: without this, ASP.NET Core happily tries to bind [FromBody]
// regardless of Content-Type and a non-JSON body just surfaces as a generic
// 400 from AuthService's own null checks. Declaring the accepted media type
// makes the framework return a proper 415 Unsupported Media Type up front.
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
            // SECURITY FIX (#6 - account enumeration qua Register): trước đây đây là
            // nhánh race-condition (2 request đăng ký cùng email gần như đồng thời,
            // unique index trên User.Email chặn request thua cuộc) và trả thẳng
            // 409 Conflict "Email đã được sử dụng." - vẫn là một oracle lộ thông tin
            // email đã tồn tại trong hệ thống, y hệt vấn đề đã sửa ở AuthService cho
            // nhánh không-race. Giờ trả 201 với response mơ hồ giống hệt nhánh thành
            // công/email-đã-tồn-tại ở AuthService.RegisterAsync, để không thể phân
            // biệt được ba trường hợp (tạo mới thật / email đã tồn tại / thua race)
            // chỉ bằng cách quan sát response.
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

    private ObjectResult Error(AuthException exception) => StatusCode(exception.StatusCode, new { message = exception.Message });
}