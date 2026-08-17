namespace ECommerce.Application;

public sealed record StripeCreatePaymentIntentRequest(
    int OrderId,
    long AmountVnd,
    string Currency,
    string CustomerEmail,
    string IdempotencyKey
);

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string Status
);

public sealed record StripeRefundResult(
    bool Success,
    string? RefundId,
    string? Message
);

public sealed record StripeWebhookEvent(
    string EventType,
    string PaymentIntentId,
    string? OrderIdFromMetadata,
    long AmountReceived
);

public interface IStripeService
{
    /// <summary>Tạo PaymentIntent mới cho một đơn hàng.</summary>
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(StripeCreatePaymentIntentRequest request);

    /// <summary>Lấy lại trạng thái PaymentIntent hiện có (dùng khi user F5/double-click).</summary>
    Task<StripePaymentIntentResult?> RetrievePaymentIntentAsync(string paymentIntentId);

    /// <summary>Hoàn tiền toàn bộ theo PaymentIntent.</summary>
    Task<StripeRefundResult> RefundAsync(string paymentIntentId, long amountVnd);

    /// <summary>Xác thực chữ ký webhook (Stripe-Signature) và parse event PaymentIntent.</summary>
    bool TryConstructEvent(string rawJson, string signatureHeader, out StripeWebhookEvent? evt);
}