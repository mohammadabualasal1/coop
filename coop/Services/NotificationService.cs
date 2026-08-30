using coop.Model;
using Microsoft.EntityFrameworkCore;
namespace coop.Services
{
    public class NotificationService : INotificationService
    {
        private readonly CoopDbContext _dbcontext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(CoopDbContext dbcontext, ILogger<NotificationService> logger)
        {
            _dbcontext = dbcontext;
            _logger = logger;
        }

        public async Task NotifyAsync(
            Guid userId,
            string title,
            string message,
            string type,
            string? relatedEntityType = null,
            Guid? relatedEntityId = null)
        {
            var now = DateTime.UtcNow;

            _dbcontext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = now
            });

            await _dbcontext.SaveChangesAsync();

            // إرسال Push عبر FCM
            var tokens = await _dbcontext.DeviceTokens
                .Where(t => t.UserId == userId && t.IsActive)
                .Select(t => t.Token)
                .ToListAsync();

            if (tokens.Count > 0)
                await SendPushAsync(tokens, title, message);
        }

        private Task SendPushAsync(List<string> tokens, string title, string message)
        {
            // TODO: ربط Firebase Cloud Messaging الفعلي
            _logger.LogInformation("Push (محاكاة) إلى {Count} جهاز: {Title} - {Message}",
                tokens.Count, title, message);

            return Task.CompletedTask;
        }
    }
}
