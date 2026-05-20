using Website.Models;

namespace Website.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using Website.Data;
    using Website.Models;

    public interface IProductRepository
    {
        Task<(List<Product> items, int totalCount)> GetAllAsync(int page, int pageSize, string? search, int? categoryId);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
    }

    public class ProductRepository(ApplicationDbContext db) : IProductRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<(List<Product> items, int totalCount)> GetAllAsync(
            int page, int pageSize, string? search, int? categoryId)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<Product?> GetByIdAsync(int id)
        {
            return _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> AddAsync(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await _db.Entry(product).Reference(p => p.Category).LoadAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _db.Products.Update(product);
            await _db.SaveChangesAsync();
            await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
        }
    }
}
