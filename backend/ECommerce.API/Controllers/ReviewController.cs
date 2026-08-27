using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        catch (DbUpdateException)
        {
            // FIX (TC-09 - safety net): dù ReviewService.CreateReviewAsync giờ đã tự phát
            // hiện review trùng (kể cả review đã bị xóa mềm) và chặn từ trước bằng
            // ReviewException, vẫn giữ lại catch này làm lớp phòng thủ cuối cùng cho race
            // condition (2 request gửi review cho cùng OrderItem gần như đồng thời), để
            // không bao giờ lộ ra 500 thô cho khách hàng.
            return Conflict(new { message = "Sản phẩm này vừa được đánh giá ở một thao tác khác. Vui lòng tải lại trang." });
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

    [HttpGet("admin/reviews")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _reviewService.GetAllReviewsAsync(page, pageSize);
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

    [HttpDelete("admin/reviews/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        try
        {
            var result = await _reviewService.DeleteReviewAsync(id);
            return Ok(result);
        }
        catch (ReviewException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("admin/reviews/{id:int}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreReview(int id)
    {
        try
        {
            var result = await _reviewService.RestoreReviewAsync(id);
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