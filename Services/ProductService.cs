using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db) => _db = db;

    public async Task<(IEnumerable<Product> Products, int TotalCount)>
        GetAllAsync(string? search = null, int? categoryId = null,
                    int page = 1, int pageSize = 12)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) || p.Description.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Product> CreateAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        var existing = await _db.Products.FindAsync(product.Id);
        if (existing is null) throw new InvalidOperationException("Product not found.");

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.StockQuantity = product.StockQuantity;
        existing.CategoryId = product.CategoryId;
        existing.ImageUrl = product.ImageUrl;
        existing.IsActive = product.IsActive;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is not null)
        {
            product.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync() =>
        await _db.Categories.OrderBy(c => c.Name).ToListAsync();

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8) =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity > 0)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
}