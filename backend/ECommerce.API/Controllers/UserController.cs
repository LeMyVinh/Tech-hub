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

    // SOFT DELETE: xóa khỏi danh sách quản trị. User bị lọc bởi HasQueryFilter
    // trong AppDbContext (IsDeleted=true), dữ liệu vẫn giữ nguyên trong DB để
    // không phá vỡ lịch sử đơn hàng.
    [HttpDelete("admin/users/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            await _userService.SoftDeleteUserAsync(id);
            return Ok(new { message = "Đã xóa người dùng." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    // RESTORE: khôi phục user đã bị soft delete. Đảo IsDeleted về false để user
    // hoạt động lại bình thường và xuất hiện trong danh sách quản trị.
    [HttpPut("admin/users/{id:int}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreUser(int id)
    {
        try
        {
            await _userService.RestoreUserAsync(id);
            return Ok(new { message = "Đã khôi phục người dùng." });
        }
        catch (UserException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}