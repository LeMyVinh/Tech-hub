using System.Net;
using System.Net.Mail;
using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Email;

/// <summary>
/// Gửi email chứa mã OTP 6 chữ số để xác thực địa chỉ email sau khi đăng ký.
/// Dùng chung cấu hình SMTP với OrderConfirmationEmailSender (mục EmailSettings).
/// Nếu SMTP chưa cấu hình (môi trường Development), chỉ ghi log mã OTP thay vì gửi thật.
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

    public async Task SendAsync(User user, string otpCode, CancellationToken cancellationToken = default)
    {
        var host = _configuration["EmailSettings:SmtpServer"];

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP chưa được cấu hình (thiếu EmailSettings:SmtpServer). Mã OTP xác thực email cho {Email}: {Otp}", user.Email, otpCode);
            return;
        }

        var port = int.TryParse(_configuration["EmailSettings:Port"], out var configuredPort) ? configuredPort : 587;
        var from = _configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("EmailSettings:SenderEmail must be configured when SMTP is enabled.");

        using var message = new MailMessage(from, user.Email)
        {
            Subject = "TechHub - Mã xác thực email của bạn",
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
            Body = $@"
                <div style=""font-family:Arial,sans-serif;max-width:480px;margin:0 auto;color:#1e293b;"">
                    <div style=""background:#2563eb;padding:20px;text-align:center;"">
                        <h1 style=""color:#ffffff;margin:0;font-size:20px;"">TECHHUB</h1>
                    </div>
                    <div style=""padding:24px;text-align:center;"">
                        <h2 style=""text-align:left;"">Xin chào {user.FullName},</h2>
                        <p style=""text-align:left;"">Cảm ơn bạn đã đăng ký tài khoản TechHub. Vui lòng nhập mã bên dưới để xác thực email và hoàn tất đăng ký:</p>
                        <p style=""font-size:34px;font-weight:800;letter-spacing:10px;color:#2563eb;margin:24px 0;"">{otpCode}</p>
                        <p style=""text-align:left;color:#64748b;font-size:13px;"">Mã có hiệu lực trong 10 phút. Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email.</p>
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
            _logger.LogInformation("Đã gửi mã OTP xác thực cho {Email}.", user.Email);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex,
                "Gửi SMTP thất bại (StatusCode={StatusCode}) khi gửi mã OTP tới {Email}. " +
                "Kiểm tra lại: (1) EmailSettings:Password phải là Gmail App Password 16 ký tự, " +
                "(2) tài khoản Gmail đã bật 2-Step Verification, (3) cổng {Port} không bị firewall chặn.",
                ex.StatusCode, user.Email, port);
            throw;
        }
    }
}