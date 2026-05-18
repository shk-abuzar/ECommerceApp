namespace ECommerceApp.Services;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(
        List<(string Name, decimal Price, int Quantity)> items,
        string successUrl,
        string cancelUrl,
        string customerEmail);

    Task<bool> VerifyPaymentAsync(string sessionId);
}
