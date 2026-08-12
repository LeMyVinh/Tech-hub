using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.PaymentGateway;

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
            dataToSign.Length--;

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

    public async Task<VnpayRefundResult> RefundAsync(VnpayRefundRequest request, CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid().ToString("N")[..20];
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
        var amount = (request.AmountVnd * 100).ToString();
        var transactionType = request.FullRefund ? "02" : "03";
        var transactionNo = string.IsNullOrWhiteSpace(request.TransactionNo) ? "0" : request.TransactionNo;
        var version = string.IsNullOrWhiteSpace(_settings.Version) ? "2.1.0" : _settings.Version;
        const string command = "refund";

        // Hash theo tài liệu VNPay refund: nối bằng |
        var data =
            $"{requestId}|{version}|{command}|{_settings.TmnCode}|{transactionType}|" +
            $"{request.TxnRef}|{amount}|{transactionNo}|{request.TransactionDate}|" +
            $"{request.CreateBy}|{createDate}|{request.ClientIp}|{request.OrderInfo}";

        var secureHash = HmacSha512(data);

        var body = new Dictionary<string, string>
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = version,
            ["vnp_Command"] = command,
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_TransactionType"] = transactionType,
            ["vnp_TxnRef"] = request.TxnRef,
            ["vnp_Amount"] = amount,
            ["vnp_TransactionNo"] = transactionNo,
            ["vnp_TransactionDate"] = request.TransactionDate,
            ["vnp_CreateBy"] = request.CreateBy,
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = request.ClientIp,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_SecureHash"] = secureHash
        };

        var apiUrl = string.IsNullOrWhiteSpace(_settings.ApiUrl)
            ? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
            : _settings.ApiUrl;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await http.PostAsJsonAsync(apiUrl, body, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return new VnpayRefundResult(
                false,
                "HTTP",
                $"HTTP {(int)response.StatusCode}: {json}",
                null);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string Get(string name)
        {
            if (!root.TryGetProperty(name, out var p)) return "";
            return p.ValueKind == JsonValueKind.String ? (p.GetString() ?? "") : p.ToString();
        }

        var code = Get("vnp_ResponseCode");
        var message = Get("vnp_Message");
        var responseIdRaw = Get("vnp_ResponseId");
        var responseId = string.IsNullOrWhiteSpace(responseIdRaw) ? null : responseIdRaw;

        return new VnpayRefundResult(
            Success: code == "00",
            ResponseCode: code,
            Message: string.IsNullOrWhiteSpace(message) ? $"ResponseCode={code}" : message,
            ResponseId: responseId);
    }
}