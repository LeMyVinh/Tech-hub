namespace ECommerce.Infrastructure.PaymentGateway;

public sealed class VnpaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "2.1.0";
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;

    /// <summary>
    /// API querydr / refund.
    /// Sandbox: https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
    /// </summary>
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
}