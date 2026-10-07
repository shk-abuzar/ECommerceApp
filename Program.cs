using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

string? connectionString = rawConnectionString;
if (!string.IsNullOrWhiteSpace(rawConnectionString) &&
    (rawConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
     rawConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        var uri = new Uri(rawConnectionString);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        connectionString = rawConnectionString;
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ─── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ── FIX #20: After login redirect back to the page they came from ─────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // ReturnUrl is automatically preserved by the middleware — no extra config needed.
    // This just makes the login/access-denied paths explicit.
});

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ─── Session ─────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // extended for Stripe redirect
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None; // needed for Stripe redirect
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ─── Seed data ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try { await SeedData.InitializeAsync(services); }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

// ─── Middleware ───────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ── Merge guest cart into user cart on first request after login ─────────────
app.Use(async (context, next) =>
{
    const string cartSessionKey = "CartSessionId";
    const string mergedKey = "CartMerged";

    var user = context.User;

    if (user.Identity?.IsAuthenticated == true
        && context.Session.GetString(mergedKey) == null)
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var sessionCartId = context.Session.GetString(cartSessionKey);

        // Only merge if there IS a guest cart session and a valid userId
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionCartId))
        {
            try
            {
                var cartService = context.RequestServices.GetRequiredService<ICartService>();
                await cartService.MergeGuestCartAsync(sessionCartId, userId);
            }
            catch { /* fail silently */ }
        }

        // Mark merged regardless — so we don't try again this session
        context.Session.Remove(cartSessionKey);
        context.Session.SetString(mergedKey, "1");
    }

    await next();
});

// ─── Routes ──────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();