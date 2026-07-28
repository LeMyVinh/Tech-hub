using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/wishlist")]
[Authorize(Roles = "Customer")]
public sealed class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        try
        {
            var userId = GetUserId();
            var result = await _wishlistService.GetWishlistAsync(userId);
            return Ok(result);
        }
        catch (WishlistException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _wishlistService.AddToWishlistAsync(userId, request.ProductId);
            return Ok(result);
        }
        catch (WishlistException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            return Ok(result);
        }
        catch (WishlistException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("{productId:int}/move-to-cart")]
    public async Task<IActionResult> MoveToCart(int productId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _wishlistService.MoveToCartAsync(userId, productId);
            return Ok(result);
        }
        catch (WishlistException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public sealed record AddToWishlistRequest(int ProductId);
