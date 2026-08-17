using ECommerce.Application;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerce.Infrastructure.PaymentGateway;

public sealed class StripeService : IStripeService
{
    private readonly StripeSettings _settings;

    public StripeService(IOptions<StripeSettings> options)
    {
        _settings = options.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }
    public string PublishableKey => _settings.PublishableKey;

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(StripeCreatePaymentIntentRequest request)
    {
        var service = new PaymentIntentService();

        // VND là zero-decimal currency trong Stripe: Amount = số tiền nguyên (không nhân 100).
        var options = new PaymentIntentCreateOptions
        {
            Amount = request.AmountVnd,
            Currency = request.Currency,
            ReceiptEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail,
            Metadata = new Dictionary<string, string>
            {
                ["OrderId"] = request.OrderId.ToString()
            },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };

        var requestOptions = new RequestOptions { IdempotencyKey = request.IdempotencyKey };

        var intent = await service.CreateAsync(options, requestOptions);

        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret, intent.Status);
    }

    public async Task<StripePaymentIntentResult?> RetrievePaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId);
        if (intent is null) return null;

        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret, intent.Status);
    }

    public async Task<StripeRefundResult> RefundAsync(string paymentIntentId, long amountVnd)
    {
        try
        {
            var service = new RefundService();
            var refund = await service.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amountVnd
            });

            var success = refund.Status is "succeeded" or "pending";
            return new StripeRefundResult(success, refund.Id, refund.Status);
        }
        catch (StripeException ex)
        {
            return new StripeRefundResult(false, null, ex.StripeError?.Message ?? ex.Message);
        }
    }

    public bool TryConstructEvent(string rawJson, string signatureHeader, out StripeWebhookEvent? evt)
    {
        evt = null;
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(rawJson, signatureHeader, _settings.WebhookSecret);

            if (stripeEvent.Data.Object is not PaymentIntent intent)
                return false;

            intent.Metadata.TryGetValue("OrderId", out var orderIdFromMetadata);

            evt = new StripeWebhookEvent(
                stripeEvent.Type,
                intent.Id,
                orderIdFromMetadata,
                intent.AmountReceived
            );
            return true;
        }
        catch
        {
            // Chữ ký sai / payload hỏng -> coi như không hợp lệ, KHÔNG throw ra ngoài.
            return false;
        }
    }
}