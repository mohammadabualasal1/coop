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

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
