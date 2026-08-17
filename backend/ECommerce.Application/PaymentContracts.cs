namespace ECommerce.Application;

public sealed record CreatePaymentRequest(
    int OrderId,
    string Method
);

public sealed record VnpayCallbackRequest(
    string vnp_TmnCode,
    string vnp_Amount,
    string vnp_BankCode,
    string vnp_BankTranNo,
    string vnp_CardType,
    string vnp_OrderInfo,
    string vnp_PayDate,
    string vnp_ResponseCode,
    string vnp_TxnRef,
    string vnp_TransactionNo,
    string vnp_TransactionStatus,
    string vnp_SecureHash
);

// ==== Credit Card (Stripe) ====

public sealed record CreateCreditCardPaymentRequest(int OrderId);

public sealed record CreditCardPaymentResponse(
    int PaymentId,
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey
);

public sealed class PaymentException : Exception
{
    public PaymentException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    public int StatusCode { get; }
}