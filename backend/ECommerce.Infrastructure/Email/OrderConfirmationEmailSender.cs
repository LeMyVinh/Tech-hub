using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Email;

/// <summary>
/// Gửi email xác nhận thanh toán VNPay thành công. Dùng chung cấu hình SMTP với
/// PasswordResetEmailSender (mục Email trong appsettings). Nếu SMTP chưa cấu hình
/// (môi trường Development), chỉ ghi log thay vì gửi thật.
/// </summary>
public sealed class OrderConfirmationEmailSender : IOrderConfirmationEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderConfirmationEmailSender> _logger;

    public OrderConfirmationEmailSender(IConfiguration configuration, ILogger<OrderConfirmationEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPaymentSuccessEmailAsync(Order order, CancellationToken cancellationToken = default)
    {
        var toEmail = order.User?.Email;
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Không thể gửi email xác nhận: Order #{OrderId} không có thông tin email người dùng.", order.Id);
            return;
        }

        var host = _configuration["EmailSettings:SmtpServer"];
        var subject = $"TechHub - Xác nhận thanh toán thành công đơn hàng #{order.Id.ToString().PadLeft(8, '0')}";
        var body = BuildEmailBody(order);

        if (string.IsNullOrWhiteSpace(host))
        {
            // Chưa cấu hình SMTP (vd: môi trường Development) -> chỉ log lại nội dung
            _logger.LogWarning(
                "SMTP chưa được cấu hình (thiếu EmailSettings:SmtpServer). Email xác nhận thanh toán cho {Email} (Order #{OrderId}):\n{Body}",
                toEmail, order.Id, body);
            return;
        }

        var port = int.TryParse(_configuration["EmailSettings:Port"], out var configuredPort) ? configuredPort : 587;
        var from = _configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("EmailSettings:SenderEmail must be configured when SMTP is enabled.");

        using var message = new MailMessage(from, toEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };

        // BUG FIX: trước đây đọc nhầm section "Email:EnableSsl" (không tồn tại trong appsettings),
        // đúng ra phải đọc "EmailSettings:EnableSsl". Gmail luôn yêu cầu STARTTLS ở cổng 587
        // nên nếu không cấu hình thì mặc định vẫn bật true, nhưng để rõ ràng và tránh nhầm lẫn
        // sau này, đọc đúng section + fallback an toàn về true.
        var enableSsl = !bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var parsedSsl) || parsedSsl;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };

        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];
        if (!string.IsNullOrEmpty(username))
            client.Credentials = new NetworkCredential(username, password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Đã gửi email xác nhận thanh toán cho {Email} (Order #{OrderId}).", toEmail, order.Id);
        }
        catch (SmtpException ex)
        {
            // Log chi tiết StatusCode để dễ chẩn đoán: sai App Password, tài khoản chưa bật
            // 2-Step Verification, hoặc mạng chặn cổng SMTP.
            _logger.LogError(ex,
                "Gửi SMTP thất bại (StatusCode={StatusCode}) cho Order #{OrderId} tới {Email}. " +
                "Kiểm tra lại: (1) EmailSettings:Password phải là Gmail App Password 16 ký tự, " +
                "(2) tài khoản Gmail đã bật 2-Step Verification, (3) cổng {Port} không bị firewall chặn.",
                ex.StatusCode, order.Id, toEmail, port);
            throw;
        }
    }

    private static string BuildEmailBody(Order order)
    {
        var vnd = new CultureInfo("vi-VN");
        string FormatVnd(decimal amount) => amount.ToString("#,##0", vnd) + "đ";

        var itemsHtml = new StringBuilder();
        foreach (var item in order.OrderItems)
        {
            itemsHtml.Append($@"
                <tr>
                    <td style=""padding:8px;border-bottom:1px solid #e2e8f0;"">{item.ProductVariant.Product.Name}</td>
                    <td style=""padding:8px;border-bottom:1px solid #e2e8f0;"">{item.ProductVariant.VariantName}</td>
                    <td style=""padding:8px;border-bottom:1px solid #e2e8f0;text-align:center;"">{item.Quantity}</td>
                    <td style=""padding:8px;border-bottom:1px solid #e2e8f0;text-align:right;"">{FormatVnd(item.UnitPrice * item.Quantity)}</td>
                </tr>");
        }

        var transactionCode = order.Payment?.TransactionCode ?? "N/A";
        var paidAt = order.Payment?.PaidAt?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");

        return $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#1e293b;"">
                <div style=""background:#2563eb;padding:20px;text-align:center;"">
                    <h1 style=""color:#ffffff;margin:0;font-size:20px;"">TECHHUB</h1>
                </div>
                <div style=""padding:24px;"">
                    <h2 style=""color:#059669;"">✓ Thanh toán thành công</h2>
                    <p>Xin chào <strong>{order.User.FullName}</strong>,</p>
                    <p>Chúng tôi đã nhận được thanh toán qua VNPay cho đơn hàng của bạn. Chi tiết như sau:</p>
                    <table style=""width:100%;background:#f8fafc;padding:12px;border-radius:8px;border-collapse:collapse;margin:16px 0;"">
                        <tr><td style=""padding:4px 0;""><strong>Mã đơn hàng:</strong></td><td>#{order.Id.ToString().PadLeft(8, '0')}</td></tr>
                        <tr><td style=""padding:4px 0;""><strong>Mã giao dịch VNPay:</strong></td><td>{transactionCode}</td></tr>
                        <tr><td style=""padding:4px 0;""><strong>Thời gian thanh toán:</strong></td><td>{paidAt}</td></tr>
                        <tr><td style=""padding:4px 0;""><strong>Trạng thái đơn hàng:</strong></td><td>Đã xác nhận</td></tr>
                    </table>

                    <table style=""width:100%;border-collapse:collapse;margin-bottom:16px;"">
                        <thead>
                            <tr style=""background:#f1f5f9;"">
                                <th style=""padding:8px;text-align:left;"">Sản phẩm</th>
                                <th style=""padding:8px;text-align:left;"">Biến thể</th>
                                <th style=""padding:8px;text-align:center;"">SL</th>
                                <th style=""padding:8px;text-align:right;"">Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>{itemsHtml}</tbody>
                    </table>

                    <p style=""text-align:right;font-size:18px;font-weight:bold;color:#dc2626;"">
                        Tổng cộng: {FormatVnd(order.TotalAmount)}
                    </p>

                    <p style=""margin-top:24px;"">Giao đến địa chỉ: {order.Address.DetailAddress}, {order.Address.Ward}, {order.Address.District}, {order.Address.Province}</p>

                    <p style=""margin-top:24px;color:#64748b;font-size:13px;"">
                        Cảm ơn bạn đã mua sắm tại TechHub. Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ bộ phận hỗ trợ.
                    </p>
                </div>
            </div>";
    }
}