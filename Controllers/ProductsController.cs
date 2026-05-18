using ECommerceApp.Models;
using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    public ProductsController(IProductService ps) => _productService = ps;

    // GET /Products
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var vm = new ProductListViewModel
        {
            Products   = await _productService.GetAllAsync(search, categoryId),
            Categories = await _productService.GetCategoriesAsync(),
            Search     = search,
            CategoryId = categoryId
        };
        return View(vm);
    }

    // GET /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();
        return View(new ProductDetailViewModel { Product = product });
    }

    // ── Admin: Create ─────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _productService.GetCategoriesAsync();
        return View();
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            return View(product);
        }
        await _productService.CreateAsync(product);
        TempData["Success"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Admin: Edit ───────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();
        ViewBag.Categories = await _productService.GetCategoriesAsync();
        return View(product);
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            return View(product);
        }
        await _productService.UpdateAsync(product);
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    // ── Admin: Delete ─────────────────────────────────────────────────────────
    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }
}
