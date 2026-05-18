
namespace Website.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using Website.Data;
    using Website.Models;
    public interface ICategoryRepository
    {
        Task<(List<Category> items, int totalCount)> GetAllAsync(int page, int pageSize, string? search);
        Task<Category?> GetByIdAsync(int id);
        Task<Category> AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        Task<bool> ExistsAsync(int id);
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _db;

        public CategoryRepository(ApplicationDbContext db) => _db = db;

        public async Task<(List<Category> items, int totalCount)> GetAllAsync(int page, int pageSize, string? search)
        {
            var query = _db.Categories.Include(c => c.Products).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search) ||
                                         (c.Description != null && c.Description.Contains(search)));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Category?> GetByIdAsync(int id) =>
            await _db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Category> AddAsync(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task UpdateAsync(Category category)
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Category category)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _db.Categories.AnyAsync(c => c.Id == id);
    }
}
