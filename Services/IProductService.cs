using ECommerceApp.Models;
using ECommerceApp.ViewModels;

namespace ECommerceApp.Services;

public interface IProductService
{
    Task<(IEnumerable<Product> Products, int TotalCount)>
                                GetAllAsync(string? search = null, int? categoryId = null,
                                            int page = 1, int pageSize = 12);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8);
}