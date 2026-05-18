using ECommerceApp.Models;

namespace ECommerceApp.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockCount { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<TopProductItem> TopProducts { get; set; } = new();
    public List<OrderStatusCount> OrdersByStatus { get; set; } = new();
}

public class TopProductItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusCount
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class AdminUserItem
{
    public ApplicationUser User { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}