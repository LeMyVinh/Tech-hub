using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/admin/brands")]
[Authorize(Roles = "Admin")]
public sealed class AdminBrandController : ControllerBase
{
    private readonly IBrandService _brandService;

    public AdminBrandController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request)
    {
        try
        {
            var result = await _brandService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandRequest request)
    {
        try
        {
            var result = await _brandService.UpdateAsync(id, request);
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
            var message = await _brandService.DeleteAsync(id);
            return Ok(new { message });
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _brandService.GetAllAsync(includeInactive: true);
        return Ok(result);
    }
}
