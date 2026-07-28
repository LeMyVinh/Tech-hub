using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.CreatePaymentAsync(userId, request);
            return Ok(result);
        }
        catch (PaymentException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayCallback([FromQuery] VnpayCallbackRequest request)
    {
        try
        {
            var result = await _paymentService.ProcessVnpayCallbackAsync(request);
            return Ok(result);
        }
        catch (PaymentException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetPaymentByOrderId(int orderId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _paymentService.GetPaymentByOrderIdAsync(userId, orderId);
            if (result is null)
                return NotFound(new { message = "Thanh toán không tồn tại." });
            return Ok(result);
        }
        catch (PaymentException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
