namespace ECommerce.Application;

public sealed record VnpayCreateRequest(
    int OrderId,
    string TxnRef,
    long AmountVnd,
    string OrderInfo,
    string ClientIp,
    string ReturnUrl
);

public sealed record VnpayRefundRequest(
    string TxnRef,
    long AmountVnd,
    string TransactionNo,
    string TransactionDate,
    string CreateBy,
    string ClientIp,
    string OrderInfo,
    bool FullRefund = true
);

public sealed record VnpayRefundResult(
    bool Success,
    string ResponseCode,
    string Message,
    string? ResponseId
);

public interface IVnpayService
{
    /// <summary>Build the signed VNPay payment URL to redirect the customer to.</summary>
    string CreatePaymentUrl(VnpayCreateRequest request);

    /// <summary>Verify vnp_SecureHash of an incoming VNPay callback/return query.</summary>
    bool ValidateSignature(IDictionary<string, string> queryParams);

    /// <summary>HMAC-SHA512 hex using the configured HashSecret.</summary>
    string HmacSha512(string data);

    /// <summary>Gọi API hoàn tiền VNPay (vnp_Command = refund).</summary>
    Task<VnpayRefundResult> RefundAsync(VnpayRefundRequest request, CancellationToken ct = default);
}