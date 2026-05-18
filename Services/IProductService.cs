using ECommerceApp.Models;

namespace ECommerceApp.Services;

public interface IProductService
{
    Task<IEnumerable<Product>>  GetAllAsync(string? search = null, int? categoryId = null);
    Task<Product?>              GetByIdAsync(int id);
    Task<Product>               CreateAsync(Product product);
    Task<Product>               UpdateAsync(Product product);
    Task                        DeleteAsync(int id);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<IEnumerable<Product>>  GetFeaturedAsync(int count = 8);
}
