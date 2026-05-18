
namespace Website.Services
{
    using Website.Data;
    using Website.Dtos;
    using Website.Models;
    using Website.Repositories;
    using static Website.Dtos.DTOs;

    public interface ICategoryService
    {
        Task<PagedResult<CategoryResponseDto>> GetAllAsync(int page, int pageSize, string? search);
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);
        Task<CategoryResponseDto?> UpdateAsync(int id, CategoryUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo) => _repo = repo;

        public async Task<PagedResult<CategoryResponseDto>> GetAllAsync(int page, int pageSize, string? search)
        {
            var (items, totalCount) = await _repo.GetAllAsync(page, pageSize, search);

            return new PagedResult<CategoryResponseDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id);
            return category is null ? null : MapToDto(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim()
            };

            var created = await _repo.AddAsync(category);
            return MapToDto(created);
        }

        public async Task<CategoryResponseDto?> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category is null) return null;

            category.Name = dto.Name.Trim();
            category.Description = dto.Description?.Trim();

            await _repo.UpdateAsync(category);
            return MapToDto(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category is null) return false;

            if (category.Products.Any())
                throw new InvalidOperationException(
                    $"Danh mục '{category.Name}' đang có {category.Products.Count} sản phẩm, không thể xóa.");

            await _repo.DeleteAsync(category);
            return true;
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _repo.ExistsAsync(id);

        private static CategoryResponseDto MapToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ProductCount = c.Products.Count
        };
    }
}
