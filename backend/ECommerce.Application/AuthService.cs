using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ECommerce.Domain;

namespace ECommerce.Application;

public class AuthService : IAuthService
{
    private const int PasswordResetLifetimeMinutes = 15;
    private const int RefreshTokenLifetimeDays = 7;
    private const int PasswordMaxLength = 100;
    private const int EmailMaxLength = 254;

    // EMAIL VERIFICATION (OTP): mã 6 chữ số, hiệu lực trong 10 phút.
    private const int OtpLength = 6;
    private const int OtpLifetimeMinutes = 10;

    // AUTH-079 fix: brute-force lockout thresholds.
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly Regex EmailRegex = new(
        @"^(?!.*\.\.)[^\s@.][^\s@]*@[^\s@.][^\s@]*\.[^\s@.][^\s@]*$",
        RegexOptions.Compiled);

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokens;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IPasswordResetEmailSender _emailSender;
    private readonly IEmailVerificationEmailSender _verificationEmailSender;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        IEmailVerificationTokenRepository emailVerificationTokens,
        IJwtTokenGenerator jwt,
        IPasswordResetEmailSender emailSender,
        IEmailVerificationEmailSender verificationEmailSender)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _passwordResetTokens = passwordResetTokens;
        _emailVerificationTokens = emailVerificationTokens;
        _jwt = jwt;
        _emailSender = emailSender;
        _verificationEmailSender = verificationEmailSender;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var fullName = Require(request.FullName, "Vui lòng nhập họ tên.");
        var email = NormalizeAndValidateEmail(request.Email);
        ValidatePassword(request.Password);

        // Chống account enumeration: nếu email đã tồn tại, vẫn trả về response giống
        // hệt trường hợp tạo mới thành công, không có exception/mã lỗi khác biệt.
        // Không gửi OTP trong trường hợp này -- chủ tài khoản thật (nếu có) đã có email của họ.
        var existing = await _users.GetByEmailAsync(email);
        if (existing is not null)
        {
            return new RegisterResponse(0, fullName, email);
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            RoleId = await _roles.GetRoleIdByNameAsync("Customer"),
            IsActive = true,
            EmailVerified = false
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        // EMAIL VERIFICATION (OTP): gửi mã OTP ngay sau khi tạo tài khoản.
        await SendVerificationEmailAsync(user);

        return new RegisterResponse(user.Id, user.FullName, user.Email);
        // NOTE: nhánh race-condition (2 request đăng ký cùng email gần như đồng thời)
        // vẫn có thể ném DbUpdateException do unique index trên User.Email, được xử lý
        // ở AuthController.Register (catch DbUpdateException) với response mơ hồ tương tự.
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var password = Require(request.Password, "Email và mật khẩu không được để trống.");
        var user = await _users.GetByEmailAsync(email);

        if (user is null)
            throw new AuthException(401, "Sai email hoặc mật khẩu.");

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            throw new AuthException(423, "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau ít phút.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            await _users.SaveChangesAsync();
            throw new AuthException(401, "Sai email hoặc mật khẩu.");
        }

        if (user.IsActive != true)
            throw new AuthException(403, "Tài khoản của bạn đã bị khoá.");

        if (!user.EmailVerified)
            throw new AuthException(403, "Email của bạn chưa được xác thực. Vui lòng nhập mã OTP đã gửi tới email (hoặc bấm 'Gửi lại mã').");

        if (user.FailedLoginAttempts > 0 || user.LockedUntil.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _users.SaveChangesAsync();
        }

        return await IssueTokensAsync(user);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request)
    {
        var tokenValue = Require(request.RefreshToken, "Refresh token không được để trống.");
        var refreshToken = await _refreshTokens.GetByTokenAsync(tokenValue);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiredAt <= DateTime.UtcNow || refreshToken.User.IsActive != true)
            throw new AuthException(401, "Refresh token không hợp lệ hoặc đã hết hạn.");

        var claimed = await _refreshTokens.TryRevokeAsync(refreshToken.Id);
        if (!claimed)
            throw new AuthException(401, "Refresh token đã được sử dụng ở một request khác.");

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
        if (user is null) return;

        await _passwordResetTokens.InvalidateActiveTokensByUserIdAsync(user.Id);

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

        await _refreshTokens.RevokeAllByUserIdAsync(resetToken.UserId);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var oldPassword = Require(request.OldPassword, "Mật khẩu hiện tại không được để trống.");
        ValidatePassword(request.NewPassword);

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            throw new AuthException(400, "Xác nhận mật khẩu mới không khớp.");

        var user = await _users.GetByIdAsync(userId) ?? throw new AuthException(401, "Phiên đăng nhập không hợp lệ.");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            throw new AuthException(401, "Mật khẩu hiện tại không chính xác.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword!, user.PasswordHash))
            throw new AuthException(400, "Mật khẩu mới không được trùng mật khẩu cũ.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword!);
        await _users.SaveChangesAsync();

        await _refreshTokens.RevokeAllByUserIdAsync(userId);
    }

    // EMAIL VERIFICATION (OTP)
    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var otp = Require(request.Otp, "Vui lòng nhập mã OTP.");

        if (otp.Length != OtpLength || !otp.All(char.IsDigit))
            throw new AuthException(400, "Mã OTP không hợp lệ.");

        var user = await _users.GetByEmailAsync(email);
        if (user is null)
            throw new AuthException(400, "Mã OTP không hợp lệ hoặc đã hết hạn.");

        if (user.EmailVerified)
            throw new AuthException(400, "Email này đã được xác thực trước đó.");

        var token = await _emailVerificationTokens.GetActiveByUserIdAndCodeAsync(user.Id, otp);
        if (token is null)
            throw new AuthException(400, "Mã OTP không hợp lệ hoặc đã hết hạn.");

        user.EmailVerified = true;
        token.IsUsed = true;
        await _emailVerificationTokens.SaveChangesAsync();
    }

    // EMAIL VERIFICATION (OTP)
    public async Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var user = await _users.GetByEmailAsync(email);

        // Không tiết lộ tài khoản có tồn tại hay không, cùng nguyên tắc với ForgotPasswordAsync.
        if (user is null || user.EmailVerified) return;

        await SendVerificationEmailAsync(user);
    }

    private async Task SendVerificationEmailAsync(User user)
    {
        await _emailVerificationTokens.InvalidateActiveTokensByUserIdAsync(user.Id);

        var otp = GenerateOtpCode();
        await _emailVerificationTokens.AddAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            Token = otp,
            ExpiredAt = DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes),
            IsUsed = false
        });
        await _emailVerificationTokens.SaveChangesAsync();
        await _verificationEmailSender.SendAsync(user, otp);
    }

    // Sinh mã OTP 6 chữ số ngẫu nhiên bằng CSPRNG (không dùng Random thường).
    private static string GenerateOtpCode()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000u;
        return value.ToString("D6");
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

        if (result.Length > EmailMaxLength)
            throw new AuthException(400, $"Email không được vượt quá {EmailMaxLength} ký tự.");

        if (!EmailRegex.IsMatch(result))
            throw new AuthException(400, "Email không đúng định dạng.");

        return result;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new AuthException(400, "Mật khẩu không được để trống.");

        if (password.Length < 6)
            throw new AuthException(400, "Mật khẩu phải có ít nhất 6 ký tự.");

        if (password.Length > PasswordMaxLength)
            throw new AuthException(400, $"Mật khẩu không được vượt quá {PasswordMaxLength} ký tự.");

        if (!password.Any(char.IsUpper))
            throw new AuthException(400, "Mật khẩu phải chứa ít nhất 1 chữ hoa.");

        if (!password.Any(char.IsDigit))
            throw new AuthException(400, "Mật khẩu phải chứa ít nhất 1 chữ số.");
    }
}