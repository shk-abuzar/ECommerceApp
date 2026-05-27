using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderService(
        ApplicationDbContext db,
        ICartService cartService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cartService = cartService;
        _emailService = emailService;
        _userManager = userManager;
    }

    public async Task<Order> PlaceOrderAsync(string userId, CheckoutViewModel model)
    {
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new Exception("Cart is empty.");

        if (!cart.CartItems.Any()) throw new Exception("Cart is empty.");

        // Validate stock for all items before placing
        foreach (var item in cart.CartItems)
        {
            var product = item.Product!;
            if (product.StockQuantity < item.Quantity)
                throw new Exception($"Insufficient stock for '{product.Name}'. " +
                                    $"Only {product.StockQuantity} left.");
        }

        var order = new Order
        {
            UserId = userId,
            TotalAmount = cart.TotalPrice,
            ShippingAddress = model.ShippingAddress,
            City = model.City,
            PostalCode = model.PostalCode,
            Country = model.Country,
            PhoneNumber = model.PhoneNumber,
            OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            }).ToList()
        };

        // Deduct stock
        foreach (var item in cart.CartItems)
            item.Product!.StockQuantity -= item.Quantity;

        _db.Orders.Add(order);
        await _cartService.ClearCartAsync(userId, null);
        await _db.SaveChangesAsync();

        // ── Send confirmation email ───────────────────────────────────────────
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email is not null)
            {
                var emailItems = cart.CartItems
                    .Select(ci => (ci.Product!.Name, ci.Quantity, ci.UnitPrice))
                    .ToList();

                await _emailService.SendOrderConfirmationAsync(
                    toEmail: user.Email,
                    customerName: user.UserName ?? user.Email,
                    orderId: order.Id,
                    total: order.TotalAmount,
                    items: emailItems,
                    shippingAddress: $"{model.ShippingAddress}, {model.City}, {model.Country}");
            }
        }
        catch (Exception)
        {
            // Email failure never breaks order placement
        }

        return order;
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId) =>
        await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<Order?> GetOrderByIdAsync(int id, string userId) =>
        await _db.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

    public async Task<IEnumerable<Order>> GetAllOrdersAsync() =>
        await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is not null)
        {
            order.Status = status;
            await _db.SaveChangesAsync();
        }
    }
}