using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = "Admin")]
public sealed class AdminProductController : ControllerBase
{
    private readonly IProductService _productService;

    public AdminProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        try
        {
            var result = await _productService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var result = await _productService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var message = await _productService.DeleteAsync(id);
            return Ok(new { message });
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterParams filter)
    {
        try
        {
            var result = await _productService.SearchAsync(filter, includeInactive: true);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _productService.GetDetailAsync(id, includeInactive: true);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
