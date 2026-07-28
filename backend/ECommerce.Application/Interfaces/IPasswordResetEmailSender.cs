using ECommerce.Domain;

namespace ECommerce.Application;

public interface IPasswordResetEmailSender
{
    Task SendAsync(User user, string resetToken, CancellationToken cancellationToken = default);
}
