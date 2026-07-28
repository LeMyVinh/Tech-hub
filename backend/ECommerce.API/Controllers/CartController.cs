using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize(Roles = "Customer")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            var userId = GetUserId();
            var result = await _cartService.GetCartAsync(userId);
            return Ok(result);
        }
        catch (CartException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cartService.AddToCartAsync(userId, request);
            return Ok(result);
        }
        catch (CartException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cartService.UpdateCartItemAsync(userId, id, request);
            return Ok(result);
        }
        catch (CartException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cartService.RemoveFromCartAsync(userId, id);
            return Ok(result);
        }
        catch (CartException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            var userId = GetUserId();
            var result = await _cartService.ClearCartAsync(userId);
            return Ok(result);
        }
        catch (CartException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
