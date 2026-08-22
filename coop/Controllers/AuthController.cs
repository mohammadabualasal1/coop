using System.Security.Claims;
using coop.Dtos.AuthController;
using coop.Enums;
using coop.Model;
using coop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}