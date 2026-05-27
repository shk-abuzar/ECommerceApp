using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db) => _db = db;

    public async Task<Cart> GetCartAsync(string? userId, string? sessionId)
    {
        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
            cart = await _db.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        else if (!string.IsNullOrEmpty(sessionId))
            cart = await _db.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        return cart ?? new Cart { UserId = userId ?? "", SessionId = sessionId };
    }

    public async Task AddItemAsync(string? userId, string? sessionId, int productId, int qty)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var product = await _db.Products.FindAsync(productId)
                      ?? throw new Exception("Product not found.");

        if (!product.IsActive)
            throw new Exception($"'{product.Name}' is no longer available.");

        // Work out how many the customer already has in cart
        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        var alreadyInCart = existing?.Quantity ?? 0;
        var totalWanted = alreadyInCart + qty;

        if (totalWanted > product.StockQuantity)
        {
            var available = product.StockQuantity - alreadyInCart;
            if (available <= 0)
                throw new Exception($"'{product.Name}' is already at maximum stock in your cart ({product.StockQuantity} available).");
            throw new Exception($"Only {available} more unit(s) of '{product.Name}' can be added (stock: {product.StockQuantity}).");
        }

        if (existing is not null)
            existing.Quantity += qty;
        else
            cart.CartItems.Add(new CartItem
            {
                ProductId = productId,
                Quantity = qty,
                UnitPrice = product.Price
            });

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(string? userId, string? sessionId, int cartItemId, int qty)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
        if (item is null) return;

        if (qty <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            // Check stock before updating quantity
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product is not null && qty > product.StockQuantity)
                throw new Exception($"Only {product.StockQuantity} unit(s) of '{product.Name}' are in stock.");

            item.Quantity = qty;
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(string? userId, string? sessionId, int cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        _db.CartItems.RemoveRange(cart.CartItems);
        await _db.SaveChangesAsync();
    }

    public async Task MergeGuestCartAsync(string sessionId, string userId)
    {
        var guestCart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (guestCart is null) return;

        var userCart = await GetOrCreateCartAsync(userId, null);

        foreach (var item in guestCart.CartItems)
        {
            var existing = userCart.CartItems.FirstOrDefault(ci => ci.ProductId == item.ProductId);
            if (existing is not null)
                existing.Quantity += item.Quantity;
            else
                userCart.CartItems.Add(new CartItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
        }

        _db.Carts.Remove(guestCart);
        await _db.SaveChangesAsync();
    }

    private async Task<Cart> GetOrCreateCartAsync(string? userId, string? sessionId)
    {
        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
            cart = await _db.Carts.Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        else if (!string.IsNullOrEmpty(sessionId))
            cart = await _db.Carts.Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId ?? "", SessionId = sessionId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }
}