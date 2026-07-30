using System.Net;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Payment;

/// <summary>
/// Cài đặt VNPay theo tài liệu tích hợp chính thức (sandbox):
/// - Ký request: sắp xếp tham số theo key (ordinal), URL-encode key/value, nối bằng "&amp;", HMAC-SHA512 với HashSecret.
/// - Xác thực callback: cùng thuật toán, bỏ qua vnp_SecureHash/vnp_SecureHashType khi tính lại checksum.
/// </summary>
public sealed class VnpayService : IVnpayService
{
    private readonly VnpaySettings _settings;

    public VnpayService(IOptions<VnpaySettings> options)
    {
        _settings = options.Value;
    }

    public string CreatePaymentUrl(VnpayCreateRequest request)
    {
        var vnpParams = new SortedList<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = (request.AmountVnd * 100).ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.TxnRef,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = request.ReturnUrl,
            ["vnp_IpAddr"] = request.ClientIp,
            // VNPay yêu cầu giờ GMT+7; Việt Nam không có DST nên cộng cứng 7 giờ là đủ chính xác.
            ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
        };

        var dataToSign = new StringBuilder();
        var queryString = new StringBuilder();

        foreach (var kv in vnpParams)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;

            var encodedKey = WebUtility.UrlEncode(kv.Key);
            var encodedValue = WebUtility.UrlEncode(kv.Value);
            dataToSign.Append(encodedKey).Append('=').Append(encodedValue).Append('&');
            queryString.Append(encodedKey).Append('=').Append(encodedValue).Append('&');
        }

        if (dataToSign.Length > 0)
            dataToSign.Length--; // bỏ dấu '&' cuối cùng trước khi ký

        var secureHash = HmacSha512(dataToSign.ToString());
        return $"{_settings.BaseUrl}?{queryString}vnp_SecureHash={secureHash}";
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash) || string.IsNullOrWhiteSpace(receivedHash))
            return false;

        var dataToSign = new StringBuilder();
        foreach (var kv in queryParams
            .Where(kv => kv.Key is not ("vnp_SecureHash" or "vnp_SecureHashType") && !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            dataToSign.Append(WebUtility.UrlEncode(kv.Key)).Append('=').Append(WebUtility.UrlEncode(kv.Value)).Append('&');
        }

        if (dataToSign.Length > 0)
            dataToSign.Length--;

        var computedHash = HmacSha512(dataToSign.ToString());
        return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    public string HmacSha512(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_settings.HashSecret);
        var messageBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);

        var sb = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
