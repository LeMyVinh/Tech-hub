using System.ComponentModel.DataAnnotations;
using ECommerce.Domain;

namespace ECommerce.Application;

public class AuthService : IAuthService
{
    private const int PasswordResetLifetimeMinutes = 15;
    private const int RefreshTokenLifetimeDays = 7;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IPasswordResetEmailSender _emailSender;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        IJwtTokenGenerator jwt,
        IPasswordResetEmailSender emailSender)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _passwordResetTokens = passwordResetTokens;
        _jwt = jwt;
        _emailSender = emailSender;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var fullName = Require(request.FullName, "Vui lòng nhập họ tên.");
        var email = NormalizeAndValidateEmail(request.Email);
        ValidatePassword(request.Password);

        if (await _users.GetByEmailAsync(email) is not null)
            throw new AuthException(409, "Email đã được sử dụng.");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            RoleId = await _roles.GetRoleIdByNameAsync("Customer"),
            IsActive = true
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();
        return new RegisterResponse(user.Id, user.FullName, user.Email);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var password = Require(request.Password, "Email và mật khẩu không được để trống.");
        var user = await _users.GetByEmailAsync(email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new AuthException(401, "Sai email hoặc mật khẩu.");
        if (user.IsActive != true)
            throw new AuthException(403, "Tài khoản của bạn đã bị khoá.");

        return await IssueTokensAsync(user);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request)
    {
        var tokenValue = Require(request.RefreshToken, "Refresh token không được để trống.");
        var refreshToken = await _refreshTokens.GetByTokenAsync(tokenValue);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiredAt <= DateTime.UtcNow || refreshToken.User.IsActive != true)
            throw new AuthException(401, "Refresh token không hợp lệ hoặc đã hết hạn.");

        refreshToken.IsRevoked = true;
        await _refreshTokens.SaveChangesAsync();
        return await IssueTokensAsync(refreshToken.User);
    }

    public async Task LogoutAsync(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return;
        var token = await _refreshTokens.GetByTokenAsync(request.RefreshToken);
        if (token is not null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            await _refreshTokens.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var user = await _users.GetByEmailAsync(email);
        if (user is null) return; // Do not disclose whether an account exists.

        var token = _jwt.GenerateRefreshToken();
        await _passwordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiredAt = DateTime.UtcNow.AddMinutes(PasswordResetLifetimeMinutes),
            IsUsed = false
        });
        await _passwordResetTokens.SaveChangesAsync();
        await _emailSender.SendAsync(user, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var token = Require(request.Token, "Token đặt lại mật khẩu không được để trống.");
        ValidatePassword(request.NewPassword);
        var resetToken = await _passwordResetTokens.GetByTokenAsync(token);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiredAt <= DateTime.UtcNow)
            throw new AuthException(400, "Liên kết đặt lại mật khẩu không còn hiệu lực.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword!, resetToken.User.PasswordHash))
            throw new AuthException(400, "Mật khẩu mới không được trùng mật khẩu cũ.");

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword!);
        resetToken.IsUsed = true;
        await _passwordResetTokens.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var oldPassword = Require(request.OldPassword, "Mật khẩu hiện tại không được để trống.");
        ValidatePassword(request.NewPassword);
        var user = await _users.GetByIdAsync(userId) ?? throw new AuthException(401, "Phiên đăng nhập không hợp lệ.");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            throw new AuthException(401, "Mật khẩu hiện tại không chính xác.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword!, user.PasswordHash))
            throw new AuthException(400, "Mật khẩu mới không được trùng mật khẩu cũ.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword!);
        await _users.SaveChangesAsync();
    }

    private async Task<LoginResponse> IssueTokensAsync(User user)
    {
        var refreshToken = _jwt.GenerateRefreshToken();
        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiredAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays),
            IsRevoked = false
        });
        await _refreshTokens.SaveChangesAsync();

        return new LoginResponse(
            _jwt.GenerateAccessToken(user),
            refreshToken,
            new UserSummary(user.Id, user.FullName, user.Role.Name));
    }

    private static string Require(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new AuthException(400, message) : value.Trim();

    private static string NormalizeAndValidateEmail(string? email)
    {
        var result = Require(email, "Email và mật khẩu không được để trống.").ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(result))
            throw new AuthException(400, "Email không đúng định dạng.");
        return result;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new AuthException(400, "Mật khẩu phải có ít nhất 6 ký tự.");
    }
}
