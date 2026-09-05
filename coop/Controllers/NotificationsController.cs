using coop.Dtos.NotificationsDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using coop.Dtos.MerchantBranchesController;
using coop.Dtos.MerchantsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public NotificationsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetCurrentUserId();

            var notifications = await _dbcontext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    RelatedEntityType = n.RelatedEntityType,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();

            var count = await _dbcontext.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new UnreadCountResponse { Count = count });
        }
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();

            var notification = await _dbcontext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound("الإشعار غير موجود");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbcontext.SaveChangesAsync();
            }

            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userId = GetCurrentUserId();

            var notification = await _dbcontext.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound("الإشعار غير موجود");

            _dbcontext.Notifications.Remove(notification);
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        [HttpPost("device-tokens")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("التوكن مطلوب");

            var existing = await _dbcontext.DeviceTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (existing != null)
            {
             
                existing.UserId = userId;
                existing.Platform = dto.Platform;
                existing.IsActive = true;
                existing.LastUsedAt = now;
            }
            else
            {
                _dbcontext.DeviceTokens.Add(new DeviceToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = dto.Token,
                    Platform = dto.Platform,
                    IsActive = true,
                    CreatedAt = now,
                    LastUsedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("device-tokens/{token}")]
        public async Task<IActionResult> UnregisterDeviceToken(string token)
        {
            var userId = GetCurrentUserId();

            var deviceToken = await _dbcontext.DeviceTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId);

            if (deviceToken == null)
                return NotFound("التوكن غير موجود");

            deviceToken.IsActive = false;
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
