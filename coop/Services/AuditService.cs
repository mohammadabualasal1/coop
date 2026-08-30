using coop.Model;

namespace coop.Services
{
    public class AuditService : IAuditService
    {
        private readonly CoopDbContext _dbcontext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(CoopDbContext dbcontext, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(Guid userId, string action, string entityType, Guid entityId, string? details = null)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            _dbcontext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                NewValues = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _dbcontext.SaveChangesAsync();
        }
    }
}