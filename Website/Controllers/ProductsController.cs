namespace Website.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Mvc;
    using Website.Models;
    using Website.Services;
    using static Website.Dtos.DTOs;

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductsController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _productService.GetAllAsync(page, pageSize, search, categoryId);
            return Ok(ApiResponse<PagedResult<ProductResponseDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product is null)
                return NotFound(ApiResponse<ProductResponseDto>.Fail($"Không tìm thấy sản phẩm với ID = {id}"));

            return Ok(ApiResponse<ProductResponseDto>.Ok(product));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProductCreateDto dto)
        {
            var error = await Validate(dto.Name, dto.Price, dto.Stock, dto.CategoryId);
            if (error is not null) return BadRequest(ApiResponse<object>.Fail(error));

            try
            {
                var created = await _productService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    ApiResponse<ProductResponseDto>.Ok(created, "Tạo sản phẩm thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductUpdateDto dto)
        {
            var error = await Validate(dto.Name, dto.Price, dto.Stock, dto.CategoryId);
            if (error is not null) return BadRequest(ApiResponse<object>.Fail(error));

            try
            {
                var updated = await _productService.UpdateAsync(id, dto);
                if (updated is null)
                    return NotFound(ApiResponse<ProductResponseDto>.Fail($"Không tìm thấy sản phẩm với ID = {id}"));

                return Ok(ApiResponse<ProductResponseDto>.Ok(updated, "Cập nhật sản phẩm thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);
            if (!deleted)
                return NotFound(ApiResponse<object>.Fail($"Không tìm thấy sản phẩm với ID = {id}"));

            return Ok(ApiResponse<object>.Ok(null!, "Xóa sản phẩm thành công"));
        }

        private async Task<string?> Validate(string name, decimal price, int stock, int categoryId)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Tên sản phẩm không được để trống";
            if (price < 0) return "Giá sản phẩm không được âm";
            if (stock < 0) return "Số lượng tồn kho không được âm";
            if (!await _categoryService.ExistsAsync(categoryId))
                return $"Danh mục với ID = {categoryId} không tồn tại";
            return null;
        }
    }
}
