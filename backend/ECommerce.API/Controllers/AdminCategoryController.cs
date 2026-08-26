using ECommerce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Roles = "Admin")]
public sealed class AdminCategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var result = await _categoryService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            // FIX (bug report #11): race window giữa ExistsByNameAsync và insert.
            return Conflict(new { message = "Tên danh mục vừa được sử dụng bởi một thao tác khác. Vui lòng thử lại." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var result = await _categoryService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Tên danh mục vừa được sử dụng bởi một thao tác khác. Vui lòng thử lại." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var message = await _categoryService.DeleteAsync(id);
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
            return Ok(new { message = await _categoryService.RestoreAsync(id) });
        }
        catch (CatalogException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync(includeInactive: true, includeDeleted: true);
        return Ok(result);
    }
}
