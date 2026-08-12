using ECommerce.Domain;

namespace ECommerce.Application;

public interface IEmailVerificationEmailSender
{
    Task SendAsync(User user, string verificationToken, CancellationToken cancellationToken = default);
}