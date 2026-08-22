using System.Security.Claims;
using coop.Dtos.AuthController;
using coop.Enums;
using coop.Model;
using coop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CoopDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(CoopDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
        {
            if (dto.Role == UserRole.Admin)
                return BadRequest("لا يمكن إنشاء حساب أدمن عن طريق التسجيل العام.");

            var normalizedEmail = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
                return Conflict("البريد الإلكتروني مستخدم مسبقاً.");

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                return Conflict("رقم الهاتف مستخدم مسبقاً.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = dto.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = await GenerateAuthResponseAsync(user);
            return StatusCode(201, response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("الحساب غير نشط، تواصل مع الدعم.");

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(await GenerateAuthResponseAsync(user));
        }
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto dto)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("كلمة المرور الحالية غير صحيحة.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await RevokeAllRefreshTokensAsync(user.Id);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح." });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<CurrentUserResponseDto>> UpdateProfile(UpdateProfileRequestDto dto)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
            {
                var phoneTaken = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != user.Id);
                if (phoneTaken)
                    return Conflict("رقم الهاتف مستخدم مسبقاً.");
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber.Trim();
            user.ProfileImageUrl = dto.ProfileImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToCurrentUserDto(user));
        }

        [HttpPost("send-verification-code")]
        public async Task<IActionResult> SendVerificationCode(SendVerificationCodeRequestDto dto)
        {
            var destination = dto.Destination.Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == destination.ToLower() || u.PhoneNumber == destination);

            var code = GenerateNumericCode();

            _context.VerificationCodes.Add(new VerificationCode
            {
                Id = Guid.NewGuid(),
                UserId = user?.Id,
                Destination = destination,
                Purpose = dto.Purpose,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // محاكاة الإرسال فقط - بالإنتاج الحقيقي هون بيتبعت SMS/Email فعلي بدل ما يرجع بالـ response
            return Ok(new { message = "تم إرسال رمز التحقق.", simulatedCode = code });
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode(VerifyCodeRequestDto dto)
        {
            var record = await _context.VerificationCodes
                .Where(v => v.Destination == dto.Destination && v.Purpose == dto.Purpose && v.UsedAt == null)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
                return BadRequest("لا يوجد رمز تحقق فعال لهذه الوجهة.");

            if (record.ExpiresAt < DateTime.UtcNow)
                return BadRequest("انتهت صلاحية رمز التحقق.");

            if (record.AttemptCount >= 5)
                return BadRequest("تم تجاوز عدد المحاولات المسموح، اطلب رمز جديد.");

            if (record.CodeHash != HashCode(dto.Code))
            {
                record.AttemptCount++;
                await _context.SaveChangesAsync();
                return BadRequest("رمز التحقق غير صحيح.");
            }

            record.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم التحقق من الرمز بنجاح." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // رسالة عامة سواء وجد الإيميل أو لا، عشان ما نكشف أي إيميلات مسجلة
            if (user == null)
                return Ok(new { message = "إذا كان البريد الإلكتروني مسجلاً، تم إرسال رمز إعادة التعيين." });

            var code = GenerateNumericCode();

            _context.VerificationCodes.Add(new VerificationCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Destination = normalizedEmail,
                Purpose = VerificationCodePurpose.PasswordReset,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "إذا كان البريد الإلكتروني مسجلاً، تم إرسال رمز إعادة التعيين.", simulatedCode = code });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            var record = await _context.VerificationCodes
                .Where(v => v.Destination == normalizedEmail
                         && v.Purpose == VerificationCodePurpose.PasswordReset
                         && v.UsedAt == null)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
                return BadRequest("لا يوجد رمز إعادة تعيين فعال لهذا البريد.");

            if (record.ExpiresAt < DateTime.UtcNow)
                return BadRequest("انتهت صلاحية رمز إعادة التعيين.");

            if (record.AttemptCount >= 5)
                return BadRequest("تم تجاوز عدد المحاولات المسموح، اطلب رمز جديد.");

            if (record.CodeHash != HashCode(dto.Code))
            {
                record.AttemptCount++;
                await _context.SaveChangesAsync();
                return BadRequest("رمز التحقق غير صحيح.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
                return BadRequest("المستخدم غير موجود.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            record.UsedAt = DateTime.UtcNow;

            await RevokeAllRefreshTokensAsync(user.Id);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديث كلمة المرور بنجاح." });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto dto)
        {
            var tokenHash = _jwtService.HashToken(dto.RefreshToken);

            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null || storedToken.RevokedAt != null || storedToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("رمز التحديث غير صالح أو منتهي.");

            if (storedToken.User.Status != UserStatus.Active)
                return Unauthorized("الحساب غير نشط.");

            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = storedToken.UserId,
                TokenHash = _jwtService.HashToken(newRefreshToken),
                ExpiresAt = _jwtService.GetRefreshTokenExpiry(),
                CreatedAt = DateTime.UtcNow,
                DeviceName = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByTokenId = newRefreshTokenEntity.Id;

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDto
            {
                AccessToken = _jwtService.GenerateAccessToken(storedToken.User),
                RefreshToken = newRefreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiry(),
                User = MapToCurrentUserDto(storedToken.User)
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<CurrentUserResponseDto>> Me()
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null) return NotFound();

            return Ok(MapToCurrentUserDto(user));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
        {
            var tokenHash = _jwtService.HashToken(dto.RefreshToken);
            var userId = GetCurrentUserId();

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.UserId == userId);

            if (storedToken != null && storedToken.RevokedAt == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
        {
            var refreshToken = _jwtService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = _jwtService.HashToken(refreshToken),
                ExpiresAt = _jwtService.GetRefreshTokenExpiry(),
                CreatedAt = DateTime.UtcNow,
                DeviceName = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = refreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiry(),
                User = MapToCurrentUserDto(user)
            };
        }

        private static CurrentUserResponseDto MapToCurrentUserDto(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            ProfileImageUrl = user.ProfileImageUrl
        };

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


        private static string GenerateNumericCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1000000);
            return value.ToString("D6");
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(bytes);
        }

        private async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
                token.RevokedAt = DateTime.UtcNow;
        }

    }

}