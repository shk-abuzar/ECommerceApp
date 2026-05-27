using ECommerceApp.Models;
using ECommerceApp.Services;
using ECommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;
    private const int PageSize = 12;

    public ProductsController(IProductService ps, IWebHostEnvironment env)
    {
        _productService = ps;
        _env = env;
    }

    // GET /Products
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        var (products, totalCount) =
            await _productService.GetAllAsync(search, categoryId, page, PageSize);

        var vm = new ProductListViewModel
        {
            Products = products,
            Categories = await _productService.GetCategoriesAsync(),
            Search = search,
            CategoryId = categoryId,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            TotalCount = totalCount,
            PageSize = PageSize
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

    // ── Stock API (used by live polling on Details page) ──────────────────────
    [HttpGet]
    public async Task<IActionResult> GetStock(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();
        return Json(new { stock = product.StockQuantity });
    }

    // ── Admin: Create ─────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _productService.GetCategoriesAsync();
        return View();
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            return View(product);
        }

        // Handle image upload
        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveImageAsync(imageFile);
            if (saved is null)
            {
                ModelState.AddModelError("ImageUrl", "Invalid file type. Use JPG, PNG, or WebP.");
                ViewBag.Categories = await _productService.GetCategoriesAsync();
                return View(product);
            }
            product.ImageUrl = saved;
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrl))
            product.ImageUrl = "/images/no-image.png";

        await _productService.CreateAsync(product);
        TempData["Success"] = $"Product '{product.Name}' created successfully.";
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
    public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            return View(product);
        }

        // Handle image upload — only replace if a new file was uploaded
        if (imageFile is { Length: > 0 })
        {
            var saved = await SaveImageAsync(imageFile);
            if (saved is null)
            {
                ModelState.AddModelError("ImageUrl", "Invalid file type. Use JPG, PNG, or WebP.");
                ViewBag.Categories = await _productService.GetCategoriesAsync();
                return View(product);
            }
            product.ImageUrl = saved;
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrl))
            product.ImageUrl = "/images/no-image.png";

        await _productService.UpdateAsync(product);
        TempData["Success"] = $"Product '{product.Name}' updated.";
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

    // ── Private: Save uploaded image to wwwroot/images/products/ ─────────────
    private async Task<string?> SaveImageAsync(IFormFile file)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return null;

        // Ensure directory exists
        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);

        // Unique filename to avoid collisions
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/images/products/{fileName}";
    }
}