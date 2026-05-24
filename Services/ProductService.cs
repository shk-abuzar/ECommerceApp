using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db) => _db = db;

    // =====================================================================
    // 🚧 FRONTEND DEV MODE — All methods return mock data (no DB needed)
    // ✅ TO RESTORE: Delete this file and replace with the original version
    // =====================================================================

    private static readonly List<Category> _mockCategories = new()
    {
        new Category { Id = 1, Name = "Electronics" },
        new Category { Id = 2, Name = "Clothing" },
        new Category { Id = 3, Name = "Books" },
        new Category { Id = 4, Name = "Home & Garden" },
    };

    private static readonly List<Product> _mockProducts = new()
    {
        new Product { Id = 1, Name = "Wireless Headphones",   Description = "Premium noise-cancelling headphones", Price = 129.99m, StockQuantity = 15, IsActive = true, CategoryId = 1, Category = new Category { Id = 1, Name = "Electronics" }, CreatedAt = DateTime.UtcNow.AddDays(-10) },
        new Product { Id = 2, Name = "Running Shoes",         Description = "Lightweight running shoes",           Price = 89.99m,  StockQuantity = 30, IsActive = true, CategoryId = 2, Category = new Category { Id = 2, Name = "Clothing" },     CreatedAt = DateTime.UtcNow.AddDays(-8)  },
        new Product { Id = 3, Name = "C# Programming Guide",  Description = "Complete guide to C# development",   Price = 39.99m,  StockQuantity = 50, IsActive = true, CategoryId = 3, Category = new Category { Id = 3, Name = "Books" },         CreatedAt = DateTime.UtcNow.AddDays(-6)  },
        new Product { Id = 4, Name = "Smart Watch",           Description = "Feature-packed smartwatch",          Price = 199.99m, StockQuantity = 8,  IsActive = true, CategoryId = 1, Category = new Category { Id = 1, Name = "Electronics" }, CreatedAt = DateTime.UtcNow.AddDays(-5)  },
        new Product { Id = 5, Name = "Garden Tool Set",       Description = "Complete 10-piece garden set",       Price = 59.99m,  StockQuantity = 20, IsActive = true, CategoryId = 4, Category = new Category { Id = 4, Name = "Home & Garden" }, CreatedAt = DateTime.UtcNow.AddDays(-4)  },
        new Product { Id = 6, Name = "Bluetooth Speaker",     Description = "Portable waterproof speaker",        Price = 79.99m,  StockQuantity = 12, IsActive = true, CategoryId = 1, Category = new Category { Id = 1, Name = "Electronics" }, CreatedAt = DateTime.UtcNow.AddDays(-3)  },
        new Product { Id = 7, Name = "Yoga Mat",              Description = "Non-slip premium yoga mat",          Price = 34.99m,  StockQuantity = 25, IsActive = true, CategoryId = 2, Category = new Category { Id = 2, Name = "Clothing" },     CreatedAt = DateTime.UtcNow.AddDays(-2)  },
        new Product { Id = 8, Name = "Desk Lamp",             Description = "LED adjustable desk lamp",           Price = 44.99m,  StockQuantity = 18, IsActive = true, CategoryId = 4, Category = new Category { Id = 4, Name = "Home & Garden" }, CreatedAt = DateTime.UtcNow.AddDays(-1)  },
    };

    public async Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null)
    {
        var query = _mockProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || p.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await Task.FromResult(query.OrderBy(p => p.Name).ToList());
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await Task.FromResult(_mockProducts.FirstOrDefault(p => p.Id == id));

    public async Task<Product> CreateAsync(Product product)
    {
        product.Id = _mockProducts.Max(p => p.Id) + 1;
        _mockProducts.Add(product);
        return await Task.FromResult(product);
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        var existing = _mockProducts.FirstOrDefault(p => p.Id == product.Id);
        if (existing is not null)
        {
            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
        }
        return await Task.FromResult(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = _mockProducts.FirstOrDefault(p => p.Id == id);
        if (product is not null)
            product.IsActive = false;

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync() =>
        await Task.FromResult(_mockCategories.OrderBy(c => c.Name).ToList());

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8) =>
        await Task.FromResult(
            _mockProducts
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToList()
        );
}