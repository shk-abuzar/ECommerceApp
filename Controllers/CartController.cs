using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private const string CartSessionKey = "CartSessionId";

    public CartController(ICartService cartService) => _cartService = cartService;

    private string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;

    // Gets or creates a stable session ID stored inside the session itself
    private string GetOrCreateSessionCartId()
    {
        var sessionId = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(CartSessionKey, sessionId);
        }
        return sessionId;
    }

    // GET /Cart
    public async Task<IActionResult> Index()
    {
        var sessionId = GetOrCreateSessionCartId();
        var cart = await _cartService.GetCartAsync(UserId, sessionId);
        return View(new CartViewModel { Cart = cart });
    }

    // POST /Cart/Add
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var sessionId = GetOrCreateSessionCartId();
        await _cartService.AddItemAsync(UserId, sessionId, productId, quantity);
        TempData["Success"] = "Item added to cart.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/Update
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var sessionId = GetOrCreateSessionCartId();
        await _cartService.UpdateItemAsync(UserId, sessionId, cartItemId, quantity);
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/Remove
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var sessionId = GetOrCreateSessionCartId();
        await _cartService.RemoveItemAsync(UserId, sessionId, cartItemId);
        TempData["Success"] = "Item removed.";
        return RedirectToAction(nameof(Index));
    }
}