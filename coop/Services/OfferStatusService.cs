using coop.Enums;
using Microsoft.EntityFrameworkCore;

namespace coop.Services
{
    public class OfferStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OfferStatusService> _logger;

        public OfferStatusService(IServiceProvider serviceProvider, ILogger<OfferStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateOfferStatusesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء تحديث حالات العروض");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task UpdateOfferStatusesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<CoopDbContext>();

            var now = DateTime.UtcNow;

            // العروض الموافق عليها: تصير مجدولة أو نشطة حسب وقت البداية
            var approvedOffers = await dbcontext.Offers
                .Where(o => o.Status == OfferStatus.Approved)
                .ToListAsync(stoppingToken);

            foreach (var offer in approvedOffers)
            {
                offer.Status = offer.StartAt > now ? OfferStatus.Scheduled : OfferStatus.Active;
                offer.UpdatedAt = now;
            }

            // العروض المجدولة اللي وصل وقت بدايتها
            var scheduledOffers = await dbcontext.Offers
                .Where(o => o.Status == OfferStatus.Scheduled && o.StartAt <= now)
                .ToListAsync(stoppingToken);

            foreach (var offer in scheduledOffers)
            {
                offer.Status = OfferStatus.Active;
                offer.UpdatedAt = now;
            }

            // العروض اللي انتهت مدتها
            var expiredOffers = await dbcontext.Offers
                .Where(o => o.EndAt <= now &&
                            (o.Status == OfferStatus.Active ||
                             o.Status == OfferStatus.Scheduled ||
                             o.Status == OfferStatus.Paused ||
                             o.Status == OfferStatus.SoldOut))
                .ToListAsync(stoppingToken);

            foreach (var offer in expiredOffers)
            {
                offer.Status = OfferStatus.Expired;
                offer.UpdatedAt = now;
            }

            var changes = approvedOffers.Count + scheduledOffers.Count + expiredOffers.Count;
            if (changes > 0)
            {
                await dbcontext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("تم تحديث حالة {Count} عرض", changes);
            }
        }
    }
}