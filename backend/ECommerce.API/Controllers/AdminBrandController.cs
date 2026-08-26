using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        catch (DbUpdateException)
        {
            // FIX (bug report #11): race window giữa ExistsByNameAsync và insert.
            return Conflict(new { message = "Tên thương hiệu vừa được sử dụng bởi một thao tác khác. Vui lòng thử lại." });
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
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Tên thương hiệu vừa được sử dụng bởi một thao tác khác. Vui lòng thử lại." });
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

    [HttpPut("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            var message = await _brandService.RestoreAsync(id);
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
        var result = await _brandService.GetAllAsync(includeDeleted: true);
        return Ok(result);
    }
}