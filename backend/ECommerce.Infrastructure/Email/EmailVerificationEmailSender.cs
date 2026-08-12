using System.Net;
using System.Net.Mail;
using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Email;

/// <summary>
/// Gửi email yêu cầu xác thực địa chỉ email sau khi đăng ký. Dùng chung cấu hình
/// SMTP với OrderConfirmationEmailSender (mục EmailSettings trong appsettings).
/// Nếu SMTP chưa cấu hình (môi trường Development), chỉ ghi log link thay vì gửi thật.
/// </summary>
public sealed class EmailVerificationEmailSender : IEmailVerificationEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationEmailSender> _logger;

    public EmailVerificationEmailSender(IConfiguration configuration, ILogger<EmailVerificationEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(User user, string verificationToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var verifyUrl = $"{baseUrl}/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        var host = _configuration["EmailSettings:SmtpServer"];

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP chưa được cấu hình (thiếu EmailSettings:SmtpServer). Link xác thực email cho {Email}: {VerifyUrl}", user.Email, verifyUrl);
            return;
        }

        var port = int.TryParse(_configuration["EmailSettings:Port"], out var configuredPort) ? configuredPort : 587;
        var from = _configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("EmailSettings:SenderEmail must be configured when SMTP is enabled.");

        using var message = new MailMessage(from, user.Email)
        {
            Subject = "TechHub - Xác thực địa chỉ email của bạn",
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
            Body = $@"
                <div style=""font-family:Arial,sans-serif;max-width:520px;margin:0 auto;color:#1e293b;"">
                    <div style=""background:#2563eb;padding:20px;text-align:center;"">
                        <h1 style=""color:#ffffff;margin:0;font-size:20px;"">TECHHUB</h1>
                    </div>
                    <div style=""padding:24px;"">
                        <h2>Xin chào {user.FullName},</h2>
                        <p>Cảm ơn bạn đã đăng ký tài khoản TechHub. Vui lòng xác thực địa chỉ email này để có thể đăng nhập:</p>
                        <p style=""text-align:center;margin:24px 0;"">
                            <a href=""{verifyUrl}"" style=""background:#2563eb;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block;"">Xác thực email</a>
                        </p>
                        <p>Hoặc mở liên kết sau:<br/><a href=""{verifyUrl}"">{verifyUrl}</a></p>
                        <p style=""color:#64748b;font-size:13px;margin-top:24px;"">Liên kết có hiệu lực trong 24 giờ. Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email.</p>
                    </div>
                </div>",
        };

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
            _logger.LogInformation("Đã gửi email xác thực cho {Email}.", user.Email);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex,
                "Gửi SMTP thất bại (StatusCode={StatusCode}) khi gửi email xác thực tới {Email}. " +
                "Kiểm tra lại: (1) EmailSettings:Password phải là Gmail App Password 16 ký tự, " +
                "(2) tài khoản Gmail đã bật 2-Step Verification, (3) cổng {Port} không bị firewall chặn.",
                ex.StatusCode, user.Email, port);
            throw;
        }
    }
}