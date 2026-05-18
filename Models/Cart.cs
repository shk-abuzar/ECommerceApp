using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;       // logged-in user
    public string? SessionId { get; set; }                    // guest cart
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    // Computed helpers
    public decimal TotalPrice => CartItems.Sum(i => i.SubTotal);
    public int TotalItems => CartItems.Sum(i => i.Quantity);
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart? Cart { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    // Snapshot the price at the time of adding
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    public decimal SubTotal => UnitPrice * Quantity;
}