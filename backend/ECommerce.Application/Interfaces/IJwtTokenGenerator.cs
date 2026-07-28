using ECommerce.Domain;

namespace ECommerce.Application;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
