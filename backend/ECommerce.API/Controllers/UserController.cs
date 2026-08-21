using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // FIX (Admin không truy cập được "Tài khoản của tôi"): các endpoint dưới đây
    // trước đó bị giới hạn [Authorize(Roles = "Customer")], trong khi trang
    // /account trên frontend chỉ dùng authGuard chung (cho phép mọi user đã đăng
    // nhập, kể cả Admin). Kết quả là Admin vào được UI nhưng mọi request API đều
    // trả 403, khiến trang trắng/báo lỗi. Nới quyền cho cả "Customer,Admin" để
    // Admin cũng quản lý được thông tin cá nhân + địa chỉ của chính họ.

    [HttpGet("users/me")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetUserId();
            var result = await _userService.GetUserProfileAsync(userId);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("users/me")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _userService.UpdateUserProfileAsync(userId, request);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("users/me/addresses")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetAddresses()
    {
        try
        {
            var userId = GetUserId();
            var result = await _userService.GetUserAddressesAsync(userId);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("users/me/addresses")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _userService.AddAddressAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("users/me/addresses/{id:int}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateAddressRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _userService.UpdateAddressAsync(userId, id, request);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpDelete("users/me/addresses/{id:int}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        try
        {
            var userId = GetUserId();
            await _userService.DeleteAddressAsync(userId, id);
            return Ok(new { message = "Đã xóa địa chỉ." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("users/me/addresses/{id:int}/default")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        try
        {
            var userId = GetUserId();
            await _userService.SetDefaultAddressAsync(userId, id);
            return Ok(new { message = "Đã đặt làm địa chỉ mặc định." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _userService.GetAllUsersAsync(page, pageSize);
            return Ok(result);
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("admin/users/{id:int}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LockUser(int id)
    {
        try
        {
            await _userService.LockUserAsync(id);
            return Ok(new { message = "Đã khóa tài khoản." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("admin/users/{id:int}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnlockUser(int id)
    {
        try
        {
            await _userService.UnlockUserAsync(id);
            return Ok(new { message = "Đã mở khóa tài khoản." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}