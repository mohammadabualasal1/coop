namespace coop.Services
{
    public interface IAuditService
    {
        Task LogAsync(Guid userId, string action, string entityType, Guid entityId, string? details = null);
    }
}