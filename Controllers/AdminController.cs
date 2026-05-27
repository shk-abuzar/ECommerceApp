using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        ApplicationDbContext db,
        IProductService productService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _productService = productService;
        _userManager = userManager;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var stats = new AdminDashboardViewModel
        {
            TotalProducts = await _db.Products.CountAsync(p => p.IsActive),
            TotalOrders = await _db.Orders.CountAsync(),
            TotalUsers = await _db.Users.CountAsync(),
            TotalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
            LowStockCount = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity < 10),
            RecentOrders = await _db.Orders
                                .Include(o => o.User)
                                .OrderByDescending(o => o.OrderDate)
                                .Take(5)
                                .ToListAsync(),
            TopProducts = await _db.OrderItems
                                .Include(oi => oi.Product)
                                .GroupBy(oi => oi.Product!.Name)
                                .Select(g => new TopProductItem
                                {
                                    Name = g.Key,
                                    Quantity = g.Sum(x => x.Quantity),
                                    Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                                })
                                .OrderByDescending(x => x.Revenue)
                                .Take(5)
                                .ToListAsync(),
            OrdersByStatus = await _db.Orders
                                .GroupBy(o => o.Status)
                                .Select(g => new OrderStatusCount
                                {
                                    Status = g.Key,
                                    Count = g.Count()
                                })
                                .ToListAsync()
        };
        return View(stats);
    }

    // ── Orders Management ─────────────────────────────────────────────────────
    public async Task<IActionResult> Orders(string? status)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
            query = query.Where(o => o.Status == s);

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        ViewBag.CurrentStatus = status;
        return View(orders);
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is not null)
        {
            order.Status = status;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Order #{id} status updated to {status}.";
        }
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    // ── Products Management ───────────────────────────────────────────────────
    public async Task<IActionResult> Products(string? search)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search));
        var products = await query.OrderBy(p => p.Name).ToListAsync();
        ViewBag.Search = search;
        return View(products);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is not null)
        {
            product.IsActive = !product.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Product '{product.Name}' {(product.IsActive ? "activated" : "deactivated")}.";
        }
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int id, int stock)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is not null)
        {
            product.StockQuantity = stock;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Stock updated for '{product.Name}'.";
        }
        return RedirectToAction(nameof(Products));
    }

    // ── Users Management ──────────────────────────────────────────────────────
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userList = new List<AdminUserItem>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var orderCount = await _db.Orders.CountAsync(o => o.UserId == user.Id);
            var totalSpent = await _db.Orders
                .Where(o => o.UserId == user.Id)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            userList.Add(new AdminUserItem
            {
                User = user,
                Roles = roles.ToList(),
                OrderCount = orderCount,
                TotalSpent = totalSpent
            });
        }

        return View(userList);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            if (await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["Success"] = $"Role '{role}' removed from {user.Email}.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"Role '{role}' added to {user.Email}.";
            }
        }
        return RedirectToAction(nameof(Users));
    }

    // ── Categories Management ─────────────────────────────────────────────────
    public async Task<IActionResult> Categories()
    {
        var cats = await _db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(cats);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string description)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.Categories.Add(new Category { Name = name, Description = description });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category '{name}' added.";
        }
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is not null && !cat.Products.Any())
        {
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category '{cat.Name}' deleted.";
        }
        else
        {
            TempData["Error"] = "Cannot delete a category that has products.";
        }
        return RedirectToAction(nameof(Categories));
    }
}