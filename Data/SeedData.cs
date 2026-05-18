using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // ── Roles ────────────────────────────────────────────────────────────
        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // ── Admin user ───────────────────────────────────────────────────────
        const string adminEmail = "admin@shop.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // ── Categories & Products ────────────────────────────────────────────
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Electronics",   Description = "Gadgets and devices"   },
                new() { Name = "Clothing",       Description = "Fashion and apparel"   },
                new() { Name = "Books",          Description = "Books and literature"  },
                new() { Name = "Home & Garden",  Description = "Home improvement"      }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var electronics = categories[0];
            var clothing = categories[1];
            var books = categories[2];
            var home = categories[3];

            context.Products.AddRange(

                // ── Electronics ──────────────────────────────────────────────
                new Product
                {
                    Name = "Laptop Pro 15",
                    Price = 999.99m,
                    StockQuantity = 50,
                    CategoryId = electronics.Id,
                    Description = "High-performance laptop with 16GB RAM and 512GB SSD.",
                    ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400&q=80"
                },
                new Product
                {
                    Name = "Wireless Mouse",
                    Price = 29.99m,
                    StockQuantity = 200,
                    CategoryId = electronics.Id,
                    Description = "Ergonomic wireless mouse with long battery life.",
                    ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=400&q=80"
                },
                new Product
                {
                    Name = "Bluetooth Speaker",
                    Price = 49.99m,
                    StockQuantity = 100,
                    CategoryId = electronics.Id,
                    Description = "Portable bluetooth speaker with 360° surround sound.",
                    ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=400&q=80"
                },
                new Product
                {
                    Name = "Smartphone X12",
                    Price = 799.99m,
                    StockQuantity = 75,
                    CategoryId = electronics.Id,
                    Description = "Latest smartphone with 6.7\" OLED display and 5G.",
                    ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&q=80"
                },

                // ── Clothing ─────────────────────────────────────────────────
                new Product
                {
                    Name = "Cotton T-Shirt",
                    Price = 19.99m,
                    StockQuantity = 300,
                    CategoryId = clothing.Id,
                    Description = "Premium 100% cotton t-shirt, available in multiple colors.",
                    ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400&q=80"
                },
                new Product
                {
                    Name = "Denim Jeans",
                    Price = 59.99m,
                    StockQuantity = 150,
                    CategoryId = clothing.Id,
                    Description = "Classic slim-fit denim jeans for everyday wear.",
                    ImageUrl = "https://images.unsplash.com/photo-1542272604-787c3835535d?w=400&q=80"
                },
                new Product
                {
                    Name = "Running Sneakers",
                    Price = 89.99m,
                    StockQuantity = 120,
                    CategoryId = clothing.Id,
                    Description = "Lightweight running sneakers with cushioned sole.",
                    ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&q=80"
                },

                // ── Books ─────────────────────────────────────────────────────
                new Product
                {
                    Name = "C# in Depth",
                    Price = 39.99m,
                    StockQuantity = 80,
                    CategoryId = books.Id,
                    Description = "The ultimate guide to advanced C# programming techniques.",
                    ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=400&q=80"
                },
                new Product
                {
                    Name = "Clean Code",
                    Price = 34.99m,
                    StockQuantity = 90,
                    CategoryId = books.Id,
                    Description = "A handbook of agile software craftsmanship by Robert C. Martin.",
                    ImageUrl = "https://images.unsplash.com/photo-1589998059171-988d887df646?w=400&q=80"
                },

                // ── Home & Garden ─────────────────────────────────────────────
                new Product
                {
                    Name = "Coffee Maker",
                    Price = 79.99m,
                    StockQuantity = 60,
                    CategoryId = home.Id,
                    Description = "12-cup programmable coffee maker with built-in grinder.",
                    ImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=400&q=80"
                },
                new Product
                {
                    Name = "Desk Lamp",
                    Price = 34.99m,
                    StockQuantity = 110,
                    CategoryId = home.Id,
                    Description = "LED desk lamp with adjustable brightness and USB charging port.",
                    ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=400&q=80"
                }
            );

            await context.SaveChangesAsync();
        }
    }
}