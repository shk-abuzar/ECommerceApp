namespace ECommerceApp.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, string customerName,
                                    int orderId, decimal total,
                                    List<(string Name, int Qty, decimal Price)> items,
                                    string shippingAddress);
}
