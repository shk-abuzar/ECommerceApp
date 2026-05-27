using Stripe.Checkout;

namespace ECommerceApp.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _config;

    public PaymentService(IConfiguration config)
    {
        _config = config;

        var secretKey = _config["Stripe:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("REPLACE") || secretKey.Contains("YOUR_"))
            throw new InvalidOperationException(
                "Stripe SecretKey is not configured. " +
                "Add your key to appsettings.json (Stripe:SecretKey) " +
                "or use 'dotnet user-secrets set Stripe:SecretKey sk_test_...'");

        Stripe.StripeConfiguration.ApiKey = secretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        List<(string Name, decimal Price, int Quantity)> items,
        string successUrl,
        string cancelUrl,
        string customerEmail)
    {
        var lineItems = items.Select(item => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(item.Price * 100),
                Currency = "usd",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.Name,
                },
            },
            Quantity = item.Quantity,
        }).ToList();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            CustomerEmail = customerEmail,
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url!;
    }

    public async Task<bool> VerifyPaymentAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        return session.PaymentStatus == "paid";
    }
}