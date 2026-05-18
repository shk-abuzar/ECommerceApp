using ECommerceApp.Models;
using ECommerceApp.ViewModels;

namespace ECommerceApp.Services;

public interface IOrderService
{
    Task<Order>              PlaceOrderAsync(string userId, CheckoutViewModel model);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
    Task<Order?>             GetOrderByIdAsync(int id, string userId);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task                     UpdateStatusAsync(int id, OrderStatus status);
}
