namespace ECommerce.Infrastructure.PaymentGateway;

public sealed class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>Dùng để verify chữ ký header Stripe-Signature khi nhận webhook.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}