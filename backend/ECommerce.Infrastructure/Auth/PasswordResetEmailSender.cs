using System.Net;
using System.Net.Mail;
using ECommerce.Application;
using ECommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Auth;

/// <summary>Delivers reset links through SMTP. In Development without SMTP configuration, the link is logged only.</summary>
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
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var host = _configuration["Email:SmtpHost"];

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP is not configured. Password reset link for {Email}: {ResetUrl}", user.Email, resetUrl);
            return;
        }

        var port = int.TryParse(_configuration["Email:SmtpPort"], out var configuredPort) ? configuredPort : 587;
        var from = _configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email:From must be configured when SMTP is enabled.");
        using var message = new MailMessage(from, user.Email)
        {
            Subject = "Đặt lại mật khẩu TechHub",
            Body = $"Xin chào {user.FullName},\n\nDùng liên kết sau để đặt lại mật khẩu. Liên kết có hiệu lực trong 15 phút:\n{resetUrl}",
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var enableSsl) ? enableSsl : true,
        };
        var username = _configuration["Email:Username"];
        if (!string.IsNullOrEmpty(username))
            client.Credentials = new NetworkCredential(username, _configuration["Email:Password"]);

        await client.SendMailAsync(message, cancellationToken);
    }
}
