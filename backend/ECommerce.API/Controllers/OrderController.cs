using System.Security.Claims;
using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public OrderController(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var userId = GetUserId();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var returnUrl = _configuration["Vnpay:ReturnUrl"] ?? "http://localhost:4200/payment-result";
            var result = await _orderService.CreateOrderAsync(userId, request, clientIp, returnUrl);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (OrderException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetUserId();
            var isAdmin = User.IsInRole("Admin");

            if (isAdmin)
            {
                var result = await _orderService.GetAllOrdersAsync(page, pageSize, null);
                return Ok(result);
            }
            else
            {
                var result = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
                return Ok(result);
            }
        }
        catch (OrderException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
public async Task<IActionResult> GetOrderDetail(int id)
{
    try
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _orderService.GetOrderDetailAsync(userId, id, isAdmin);
        return Ok(result);
    }
    catch (OrderException ex)
    {
        return StatusCode(ex.StatusCode, new { message = ex.Message });
    }
}

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _orderService.CancelOrderAsync(userId, id, request.Reason);
            return Ok(result);
        }
        catch (OrderException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, request);
            return Ok(result);
        }
        catch (OrderException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

public sealed record CancelOrderRequest(string? Reason);
