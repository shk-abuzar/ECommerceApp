using System.ComponentModel.DataAnnotations;
using ECommerceApp.Models;

namespace ECommerceApp.ViewModels;

// ── Products ─────────────────────────────────────────────────────────────────
public class ProductListViewModel
{
    public IEnumerable<Product>  Products   { get; set; } = Enumerable.Empty<Product>();
    public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
    public string? Search      { get; set; }
    public int?    CategoryId  { get; set; }
}

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public int     Quantity { get; set; } = 1;
}

// ── Cart ──────────────────────────────────────────────────────────────────────
public class CartViewModel
{
    public Cart     Cart    { get; set; } = null!;
    public string?  Message { get; set; }
}

// ── Checkout ──────────────────────────────────────────────────────────────────
public class CheckoutViewModel
{
    [Required, Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required, Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    [Required, Phone, Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    // Pre-filled from cart
    public Cart?    Cart        { get; set; }
    public decimal  TotalAmount { get; set; }
}
