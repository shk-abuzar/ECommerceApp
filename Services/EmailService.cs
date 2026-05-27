using System.Net;
using System.Net.Mail;
using System.Text;

namespace ECommerceApp.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(
        string toEmail,
        string customerName,
        int orderId,
        decimal total,
        List<(string Name, int Qty, decimal Price)> items,
        string shippingAddress)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPass = _config["Email:SmtpPass"];
        var fromAddr = _config["Email:FromAddress"] ?? smtpUser;
        var fromName = _config["Email:FromName"]    ?? "SHOPNET";

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
        {
            _logger.LogWarning("Email not configured — skipping order confirmation for Order #{OrderId}", orderId);
            return; // Fail silently — don't break checkout if email isn't set up
        }

        // Build HTML email body
        var itemRows = new StringBuilder();
        foreach (var (name, qty, price) in items)
        {
            itemRows.Append($@"
                <tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{name}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:center'>{qty}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right'>${price:F2}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right'>${price * qty:F2}</td>
                </tr>");
        }

        var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Montserrat,Arial,sans-serif;background:#f9f9f9;margin:0;padding:20px'>
  <div style='max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08)'>

    <!-- Header -->
    <div style='background:#1a1a1a;padding:28px 32px;text-align:center'>
      <h1 style='color:#fff;margin:0;font-size:24px;letter-spacing:4px'>SHOPNET</h1>
      <p style='color:#aaa;margin:6px 0 0;font-size:13px;letter-spacing:1px'>ORDER CONFIRMATION</p>
    </div>

    <!-- Body -->
    <div style='padding:32px'>
      <p style='font-size:16px;color:#333'>Hi {customerName},</p>
      <p style='color:#555'>Thank you for your order! We've received your payment and will process your order shortly.</p>

      <!-- Order info -->
      <div style='background:#f5f5f5;border-radius:6px;padding:16px;margin:20px 0'>
        <table style='width:100%;font-size:14px'>
          <tr>
            <td style='color:#888'>Order Number</td>
            <td style='text-align:right;font-weight:600'>#{ orderId }</td>
          </tr>
          <tr>
            <td style='color:#888;padding-top:6px'>Shipping To</td>
            <td style='text-align:right;padding-top:6px'>{shippingAddress}</td>
          </tr>
          <tr>
            <td style='color:#888;padding-top:6px'>Order Total</td>
            <td style='text-align:right;padding-top:6px;font-weight:600;color:#2e7d32'>${total:F2}</td>
          </tr>
        </table>
      </div>

      <!-- Items table -->
      <h3 style='font-size:14px;letter-spacing:1px;color:#333;margin-bottom:8px'>ITEMS ORDERED</h3>
      <table style='width:100%;border-collapse:collapse;font-size:14px'>
        <thead>
          <tr style='background:#f5f5f5'>
            <th style='padding:8px 12px;text-align:left'>Product</th>
            <th style='padding:8px 12px;text-align:center'>Qty</th>
            <th style='padding:8px 12px;text-align:right'>Price</th>
            <th style='padding:8px 12px;text-align:right'>Subtotal</th>
          </tr>
        </thead>
        <tbody>{itemRows}</tbody>
        <tfoot>
          <tr>
            <td colspan='3' style='padding:12px;text-align:right;font-weight:600'>Total</td>
            <td style='padding:12px;text-align:right;font-weight:600;color:#2e7d32'>${total:F2}</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <!-- Footer -->
    <div style='background:#f5f5f5;padding:20px 32px;text-align:center;font-size:12px;color:#aaa'>
      <p style='margin:0'>© {DateTime.Now.Year} SHOPNET — Thank you for shopping with us.</p>
    </div>
  </div>
</body>
</html>";

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mail = new MailMessage
            {
                From       = new MailAddress(fromAddr!, fromName),
                Subject    = $"Order #{orderId} Confirmed — SHOPNET",
                Body       = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Order confirmation email sent to {Email} for Order #{OrderId}", toEmail, orderId);
        }
        catch (Exception ex)
        {
            // Log but don't throw — email failure must never break checkout
            _logger.LogError(ex, "Failed to send order confirmation email for Order #{OrderId}", orderId);
        }
    }
}
