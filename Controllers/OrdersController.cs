using ECommerceApp.Models;
using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ECommerceApp.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private const string CartSessionKey = "CartSessionId";
    private const string ShippingSessionKey = "PendingShipping"; // ← FIX #05

    public OrdersController(
        IOrderService orderService,
        ICartService cartService,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _cartService = cartService;
        _paymentService = paymentService;
        _userManager = userManager;
    }

    private string UserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    private string GetSessionCartId() =>
        HttpContext.Session.GetString(CartSessionKey) ?? "";

    // GET /Orders
    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetUserOrdersAsync(UserId);
        return View(orders);
    }

    // GET /Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, UserId);
        if (order is null) return NotFound();
        return View(order);
    }

    // POST /Orders/Cancel/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, UserId);
        if (order is null) return NotFound();

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
        {
            TempData["Error"] = $"Order #{id} cannot be cancelled — it is already {order.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _orderService.UpdateStatusAsync(id, OrderStatus.Cancelled);
        TempData["Success"] = $"Order #{id} has been cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Orders/Checkout
    public async Task<IActionResult> Checkout()
    {
        var cart = await _cartService.GetCartAsync(UserId, GetSessionCartId());
        if (!cart.CartItems.Any()) return RedirectToAction("Index", "Cart");

        var vm = new CheckoutViewModel
        {
            Cart = cart,
            TotalAmount = cart.TotalPrice
        };
        return View(vm);
    }

    // POST /Orders/Checkout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Cart = await _cartService.GetCartAsync(UserId, GetSessionCartId());
            return View(vm);
        }

        // ── FIX #05: store shipping in Session (survives Stripe redirect) ──
        var shippingData = JsonSerializer.Serialize(new
        {
            vm.ShippingAddress,
            vm.City,
            vm.PostalCode,
            vm.Country,
            vm.PhoneNumber
        });
        HttpContext.Session.SetString(ShippingSessionKey, shippingData);

        var cart = await _cartService.GetCartAsync(UserId, GetSessionCartId());
        var user = await _userManager.GetUserAsync(User);
        var items = cart.CartItems
            .Select(i => (i.Product!.Name, i.UnitPrice, i.Quantity))
            .ToList();

        var successUrl = Url.Action("PaymentSuccess", "Orders", null, Request.Scheme)!;
        var cancelUrl = Url.Action("Checkout", "Orders", null, Request.Scheme)!;

        try
        {
            var stripeUrl = await _paymentService.CreateCheckoutSessionAsync(
                items, successUrl, cancelUrl, user!.Email!);
            return Redirect(stripeUrl);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Payment setup failed: " + ex.Message);
            vm.Cart = cart;
            return View(vm);
        }
    }

    // GET /Orders/PaymentSuccess?session_id=xxx
    public async Task<IActionResult> PaymentSuccess(string session_id)
    {
        var paid = await _paymentService.VerifyPaymentAsync(session_id);
        if (!paid)
        {
            TempData["Error"] = "Payment was not completed. Please try again.";
            return RedirectToAction("Checkout");
        }

        // ── FIX #05: read shipping from Session instead of TempData ──
        var shippingJson = HttpContext.Session.GetString(ShippingSessionKey);
        if (string.IsNullOrEmpty(shippingJson))
        {
            TempData["Error"] = "Shipping information was lost. Please contact support with your payment reference.";
            return RedirectToAction("Index", "Cart");
        }

        var shipping = JsonSerializer.Deserialize<ShippingData>(shippingJson)!;
        HttpContext.Session.Remove(ShippingSessionKey); // clean up

        var vm = new CheckoutViewModel
        {
            ShippingAddress = shipping.ShippingAddress,
            City = shipping.City,
            PostalCode = shipping.PostalCode,
            Country = shipping.Country,
            PhoneNumber = shipping.PhoneNumber,
        };

        try
        {
            var order = await _orderService.PlaceOrderAsync(UserId, vm);
            TempData["Success"] = $"Payment successful! Order #{order.Id} confirmed.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Cart");
        }
    }

    // GET /Orders/PaymentCancelled
    public IActionResult PaymentCancelled()
    {
        TempData["Error"] = "Payment was cancelled. Your cart is still saved.";
        return RedirectToAction("Index", "Cart");
    }

    // Private helper record for deserializing shipping session data
    private record ShippingData(
        string ShippingAddress,
        string City,
        string PostalCode,
        string Country,
        string PhoneNumber);
}