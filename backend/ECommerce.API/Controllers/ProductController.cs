using ECommerce.Application;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ProductFilterParams filter)
    {
        try
        {
            var result = await _productService.SearchAsync(filter, includeInactive: false);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        try
        {
            var result = await _productService.GetDetailAsync(id, includeInactive: false);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
