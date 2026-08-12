using System.Net;
using System.Net.Mail;
using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Auth;

/// <summary>
/// Gửi link đặt lại mật khẩu qua SMTP (EmailSettings).
/// Nếu chưa cấu hình SMTP, chỉ log link (phù hợp Development).
/// </summary>
public sealed class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetEmailSender> _logger;

    public PasswordResetEmailSender(IConfiguration configuration, ILogger<PasswordResetEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(User user, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var resetUrl = $"{baseUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}";

        // Dùng chung EmailSettings với OTP / mail xác nhận đơn
        var host = _configuration["EmailSettings:SmtpServer"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning(
                "SMTP chưa cấu hình. Link đặt lại mật khẩu cho {Email}: {ResetUrl}",
                user.Email, resetUrl);
            return;
        }

        var port = int.TryParse(_configuration["EmailSettings:Port"], out var configuredPort)
            ? configuredPort
            : 587;

        var from = _configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("EmailSettings:SenderEmail phải được cấu hình khi bật SMTP.");

        var enableSsl = !bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage(from, user.Email)
        {
            Subject = "Đặt lại mật khẩu TechHub",
            IsBodyHtml = true,
            Body = $"""
                <p>Xin chào <strong>{WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
                <p>Bạn (hoặc ai đó) đã yêu cầu đặt lại mật khẩu TechHub.</p>
                <p>Nhấn nút bên dưới để đặt mật khẩu mới. Liên kết có hiệu lực <strong>15 phút</strong>:</p>
                <p><a href="{resetUrl}" style="display:inline-block;padding:10px 18px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;">Đặt lại mật khẩu</a></p>
                <p>Hoặc copy link: <br/><a href="{resetUrl}">{resetUrl}</a></p>
                <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                """
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        var username = _configuration["EmailSettings:Username"];
        if (!string.IsNullOrEmpty(username))
            client.Credentials = new NetworkCredential(username, _configuration["EmailSettings:Password"]);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Đã gửi email đặt lại mật khẩu tới {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email đặt lại mật khẩu thất bại tới {Email}", user.Email);
            // Vẫn log link để dev test được khi SMTP lỗi
            _logger.LogWarning("Link đặt lại mật khẩu (fallback): {ResetUrl}", resetUrl);
            throw;
        }
    }
}