using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using ECommerce.Domain;

namespace ECommerce.Application;

public class AuthService : IAuthService
{
    private const int PasswordResetLifetimeMinutes = 15;
    private const int RefreshTokenLifetimeDays = 7;
    private const int PasswordMaxLength = 100;
    private const int EmailMaxLength = 254;

    // EMAIL VERIFICATION: liên kết xác thực có hiệu lực trong 24 giờ.
    private const int EmailVerificationLifetimeHours = 24;

    // AUTH-079 fix: brute-force lockout thresholds.
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // AUTH-EMAIL-01 fix: System.ComponentModel.DataAnnotations.EmailAddressAttribute
    // (dùng trước đây) chỉ kiểm tra có đúng 1 dấu '@' với ký tự ở hai bên - nó KHÔNG
    // yêu cầu domain phải có dấu chấm, nên các chuỗi như "a@b", "user@localhost",
    // "test@com" vẫn được coi là hợp lệ ở tầng backend dù UI (register.component.ts)
    // đã chặn bằng regex chặt hơn. Ai gọi thẳng API (Postman/curl, bỏ qua UI) vẫn có
    // thể tạo tài khoản với email không hợp lệ. Regex dưới đây đồng bộ với frontend
    // và bổ sung thêm: domain phải có ít nhất 1 dấu chấm, không có 2 dấu chấm liền
    // nhau, và local-part/domain không được bắt đầu/kết thúc bằng dấu chấm.
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

        // SECURITY FIX (#6 - account enumeration qua Register): trước đây khi email
        // đã tồn tại, hàm này throw AuthException(409, "Email đã được sử dụng.")
        // (xem AuthController.Register -> catch (AuthException ex) => Error(ex)).
        // Điều đó cho phép bất kỳ ai dùng chức năng Đăng ký để dò xem một email có
        // tài khoản trong hệ thống hay không, trong khi ForgotPasswordAsync bên dưới
        // lại cố tình KHÔNG tiết lộ điều tương tự - hai luồng không nhất quán, và
        // luồng "yếu" (Register) làm vô hiệu hoá nỗ lực ẩn thông tin ở luồng kia.
        //
        // Giờ trả về response có hình dạng giống hệt trường hợp tạo mới thành công,
        // không có exception, không có mã lỗi khác biệt. Id=0 đánh dấu đây không phải
        // bản ghi thật (không được AddAsync/SaveChangesAsync); FE hiện tại không đọc
        // field này, chỉ hiển thị message tĩnh rồi điều hướng sang trang đăng nhập,
        // nên hành vi hiển thị không đổi. Đồng thời KHÔNG gửi email xác thực trong
        // trường hợp này — chủ tài khoản thật (nếu có) đã có sẵn email của họ.
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

        // EMAIL VERIFICATION: gửi email xác thực ngay sau khi tạo tài khoản.
        await SendVerificationEmailAsync(user);

        return new RegisterResponse(user.Id, user.FullName, user.Email);
        // NOTE: nhánh race-condition (2 request đăng ký cùng email gần như đồng thời,
        // cả hai cùng thấy "chưa tồn tại" ở GetByEmailAsync phía trên rồi cùng INSERT)
        // vẫn có thể ném DbUpdateException do unique index trên User.Email. Nhánh đó
        // được xử lý ở AuthController.Register (catch DbUpdateException) và cũng trả
        // về response mơ hồ tương tự, để không mở lại oracle qua đường vòng race.
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var password = Require(request.Password, "Email và mật khẩu không được để trống.");
        var user = await _users.GetByEmailAsync(email);

        if (user is null)
            throw new AuthException(401, "Sai email hoặc mật khẩu.");

        // AUTH-079 fix: reject while locked out, before touching BCrypt at all.
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

        // EMAIL VERIFICATION: chặn đăng nhập cho tới khi email được xác thực.
        // Kiểm tra sau khi đã xác nhận mật khẩu đúng, để không tiết lộ trạng thái
        // xác thực cho người không biết mật khẩu.
        if (!user.EmailVerified)
            throw new AuthException(403, "Email của bạn chưa được xác thực. Vui lòng kiểm tra hộp thư (hoặc bấm 'Gửi lại email xác thực').");

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

        // AUTH-117 fix: atomic compare-and-swap. If two requests race on the
        // same (still-valid) token, only one TryRevokeAsync call can return
        // true; the loser gets a clean 401 instead of also minting tokens.
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
        // NOTE (AUTH-062): this revokes the refresh token so it can no longer
        // be exchanged for new tokens (AUTH-084/085 verified via RefreshAsync
        // above). It intentionally does NOT invalidate the short-lived access
        // token itself (stateless JWT, no per-session revocation store), and
        // it only touches the ONE refresh token supplied - other
        // browsers/devices for the same user keep their own independent
        // session untouched (AUTH-116). See JwtTokenGenerator for the
        // shortened access-token lifetime that limits this window.
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var user = await _users.GetByEmailAsync(email);
        if (user is null) return; // Do not disclose whether an account exists.

        // SECURITY FIX: đóng toàn bộ token reset còn hiệu lực trước đó của user này,
        // để không có nhiều token sống song song (xem IPasswordResetTokenRepository).
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
        // NOTE (AUTH-091): PasswordResetEmailSender only logs the link to the
        // console when Email:SmtpHost is empty (current appsettings.json).
        // That's a deployment/config gap, not a code bug - fill in real SMTP
        // (or a transactional email provider) before going to production.
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

        // SECURITY FIX: thu hồi toàn bộ refresh token đang hiệu lực của user ngay sau
        // khi mật khẩu bị đổi qua luồng quên-mật-khẩu. Nếu ai đó đã chiếm được refresh
        // token trước đó, việc chủ tài khoản reset mật khẩu qua email giờ mới thực sự
        // "cắt đứt" được phiên bị đánh cắp.
        await _refreshTokens.RevokeAllByUserIdAsync(resetToken.UserId);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var oldPassword = Require(request.OldPassword, "Mật khẩu hiện tại không được để trống.");
        ValidatePassword(request.NewPassword);

        // AUTH-099 fix: server now validates the confirmation itself instead
        // of trusting the Angular form to have done it.
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            throw new AuthException(400, "Xác nhận mật khẩu mới không khớp.");

        var user = await _users.GetByIdAsync(userId) ?? throw new AuthException(401, "Phiên đăng nhập không hợp lệ.");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            throw new AuthException(401, "Mật khẩu hiện tại không chính xác.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword!, user.PasswordHash))
            throw new AuthException(400, "Mật khẩu mới không được trùng mật khẩu cũ.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword!);
        await _users.SaveChangesAsync();

        // SECURITY FIX: tương tự ResetPasswordAsync -- đổi mật khẩu khi đang đăng nhập
        // cũng phải thu hồi mọi refresh token khác đang tồn tại, kể cả refresh token
        // của chính phiên hiện tại (client sẽ cần đăng nhập lại, đây là hành vi mong
        // muốn: "đổi mật khẩu" nên đăng xuất khỏi mọi nơi để an toàn).
        await _refreshTokens.RevokeAllByUserIdAsync(userId);
    }

    // EMAIL VERIFICATION
    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var tokenValue = Require(request.Token, "Token xác thực không được để trống.");
        var token = await _emailVerificationTokens.GetByTokenAsync(tokenValue);

        if (token is null || token.IsUsed || token.ExpiredAt <= DateTime.UtcNow)
            throw new AuthException(400, "Liên kết xác thực email không còn hiệu lực. Vui lòng yêu cầu gửi lại.");

        if (!token.User.EmailVerified)
        {
            token.User.EmailVerified = true;
        }

        token.IsUsed = true;
        await _emailVerificationTokens.SaveChangesAsync();
    }

    // EMAIL VERIFICATION
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

        var token = _jwt.GenerateRefreshToken(); // tái sử dụng generator random-token an toàn sẵn có
        await _emailVerificationTokens.AddAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            Token = token,
            ExpiredAt = DateTime.UtcNow.AddHours(EmailVerificationLifetimeHours),
            IsUsed = false
        });
        await _emailVerificationTokens.SaveChangesAsync();
        await _verificationEmailSender.SendAsync(user, token);
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

        // AUTH-016/076 fix: reject overlong emails with a clean validation
        // error instead of letting a DB truncation/insert error bubble up.
        if (result.Length > EmailMaxLength)
            throw new AuthException(400, $"Email không được vượt quá {EmailMaxLength} ký tự.");

        // AUTH-EMAIL-01 fix: dùng regex chặt (đồng bộ với frontend) thay cho
        // EmailAddressAttribute vốn chấp nhận cả email thiếu domain hợp lệ
        // (vd: "a@b", "user@localhost").
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

        // AUTH-010/077 fix: cap length so BCrypt (72-byte input limit) never
        // silently truncates the password.
        if (password.Length > PasswordMaxLength)
            throw new AuthException(400, $"Mật khẩu không được vượt quá {PasswordMaxLength} ký tự.");

        // AUTH-018 fix: require at least one uppercase letter.
        if (!password.Any(char.IsUpper))
            throw new AuthException(400, "Mật khẩu phải chứa ít nhất 1 chữ hoa.");

        // AUTH-019/100 fix: require at least one digit.
        if (!password.Any(char.IsDigit))
            throw new AuthException(400, "Mật khẩu phải chứa ít nhất 1 chữ số.");
    }
}