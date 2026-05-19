using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Website.Dtos;
using Website.Services;
using static Website.Dtos.DTOs;

namespace Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;
            var result = await _categoryService.GetAllAsync(page, pageSize, search);
            return Ok(ApiResponse<PagedResult<CategoryResponseDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<CategoryResponseDto>.Fail($"Category with ID = {id} not found"));
            }
            return Ok(ApiResponse<CategoryResponseDto>.Ok(category));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResponse<object>.Fail("Category name is required"));
            var createdCategory = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, 
                ApiResponse<CategoryResponseDto>.Ok(createdCategory, "Category created successfully"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<object>.Fail("Category name is required"));
            }
            var updatedCategory = await _categoryService.UpdateAsync(id, dto);
            if (updatedCategory == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Category with ID = {id} not found"));
            }
            return Ok(ApiResponse<CategoryResponseDto>.Ok(updatedCategory, "Category updated successfully"));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _categoryService.DeleteAsync(id);
                if (!success) return NotFound(ApiResponse<object>.Fail($"Category with ID = {id} not found"));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}