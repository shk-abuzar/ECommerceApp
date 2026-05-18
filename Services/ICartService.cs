namespace ECommerceApp.Services;

public interface ICartService
{
    Task<ECommerceApp.Models.Cart> GetCartAsync(string? userId, string? sessionId);
    Task AddItemAsync(string? userId, string? sessionId, int productId, int quantity);
    Task UpdateItemAsync(string? userId, string? sessionId, int cartItemId, int quantity);
    Task RemoveItemAsync(string? userId, string? sessionId, int cartItemId);
    Task ClearCartAsync(string? userId, string? sessionId);
    Task MergeGuestCartAsync(string sessionId, string userId);
}
