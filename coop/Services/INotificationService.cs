namespace coop.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(
            Guid userId,
            string title,
            string message,
            string type,
            string? relatedEntityType = null,
            Guid? relatedEntityId = null);
    }
}
