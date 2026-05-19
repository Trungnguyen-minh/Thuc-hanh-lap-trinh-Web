namespace Website.Services
{
    using Microsoft.EntityFrameworkCore;
    using Website.Data;
    using Website.Dtos;
    using Website.Models;
    using Website.Repositories;
    using static Website.Dtos.DTOs;

    public interface IProductService
    {

        Task<PagedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? search, int? categoryId);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);
        Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IImageService _imageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(IProductRepository repo, IImageService imageService, IHttpContextAccessor httpContextAccessor)
        {
            _repo = repo;
            _imageService = imageService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? search, int? categoryId)
        {
            var (items, totalCount) = await _repo.GetAllAsync(page, pageSize, search, categoryId);

            return new PagedResult<ProductResponseDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            return product is null ? null : MapToDto(product);
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            string? imagePath = null;
            if (dto.Image is not null)
                imagePath = await _imageService.SaveAsync(dto.Image);

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = imagePath,
                CategoryId = dto.CategoryId
            };

            var created = await _repo.AddAsync(product);
            return MapToDto(created);
        }

        public async Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product is null) return null;

            if (dto.Image is not null)
            {
                var oldPath = product.ImageUrl;
                product.ImageUrl = await _imageService.SaveAsync(dto.Image);
                _imageService.Delete(oldPath);
            }

            product.Name = dto.Name.Trim();
            product.Description = dto.Description?.Trim();
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;

            await _repo.UpdateAsync(product);
            return MapToDto(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product is null) return false;

            _imageService.Delete(product.ImageUrl);
            await _repo.DeleteAsync(product);
            return true;
        }

        private ProductResponseDto MapToDto(Product p)
        {
            string? fullImageUrl = null;
            if (!string.IsNullOrEmpty(p.ImageUrl))
            {
                var req = _httpContextAccessor.HttpContext?.Request;
                if (req is not null)
                    fullImageUrl = $"{req.Scheme}://{req.Host}{p.ImageUrl}";
            }

            return new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImagePath = p.ImageUrl,
                ImageUrl = fullImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty
            };
        }

    }
}
