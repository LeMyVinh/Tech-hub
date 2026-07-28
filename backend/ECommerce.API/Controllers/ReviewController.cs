using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost("products/{productId:int}/reviews")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateReview(int productId, [FromBody] CreateReviewRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _reviewService.CreateReviewAsync(userId, request with { ProductId = productId });
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("products/{productId:int}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _reviewService.GetProductReviewsAsync(productId, page, pageSize);
            return Ok(result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("admin/reviews/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _reviewService.GetPendingReviewsAsync(page, pageSize);
            return Ok(result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("admin/reviews/{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        try
        {
            var result = await _reviewService.ApproveReviewAsync(id);
            return Ok(result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("admin/reviews/{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectReview(int id, [FromBody] RejectReviewRequest request)
    {
        try
        {
            var result = await _reviewService.RejectReviewAsync(id, request.Reason);
            return Ok(result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public sealed record RejectReviewRequest(string? Reason);
