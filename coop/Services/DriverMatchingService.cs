using coop.Enums;
using coop.Model;
using Microsoft.EntityFrameworkCore;

namespace coop.Services
{
    public class DriverMatchingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DriverMatchingService> _logger;

        private const int OfferExpiryMinutes = 2;
        private const double SearchRadiusMeters = 15000;

        public DriverMatchingService(IServiceProvider serviceProvider, ILogger<DriverMatchingService> logger)
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
                    await MatchDriversAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء مطابقة السائقين");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task MatchDriversAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<CoopDbContext>();

            var now = DateTime.UtcNow;

            // 1) إنهاء العروض التي انتهت مهلتها
            var timedOutOffers = await dbcontext.DriverTaskOffers
                .Where(o => o.Status == DriverTaskOfferStatus.Pending && o.ExpiresAt < now)
                .ToListAsync(stoppingToken);

            foreach (var offer in timedOutOffers)
                offer.Status = DriverTaskOfferStatus.Expired;

            if (timedOutOffers.Count > 0)
                await dbcontext.SaveChangesAsync(stoppingToken);

            // 2) المهام التي تبحث عن سائق ولا يوجد لها عرض معلّق
            var tasks = await dbcontext.DeliveryTasks
                .Where(t => t.Status == DeliveryStatus.SearchingDriver && t.DriverProfileId == null)
                .Where(t => t.PickupBranch.Location != null)
                .Where(t => !dbcontext.DriverTaskOffers
                    .Any(o => o.DeliveryTaskId == t.Id && o.Status == DriverTaskOfferStatus.Pending))
                .Select(t => new
                {
                    t.Id,
                    BranchLocation = t.PickupBranch.Location!
                })
                .ToListAsync(stoppingToken);

            if (tasks.Count == 0)
                return;

            var createdCount = 0;

            foreach (var task in tasks)
            {
                // 3) أقرب سائق مؤهل — الاستعلام كله داخل قاعدة البيانات
                var nearest = await dbcontext.DriverProfiles
                    .Where(d => d.VerificationStatus == VerificationStatus.Approved)
                    .Where(d => d.IsAvailable)
                    .Where(d => d.CurrentLocation != null)
                    .Where(d => d.CurrentLocation!.IsWithinDistance(task.BranchLocation, SearchRadiusMeters))
                    .Where(d => !dbcontext.DeliveryTasks
                        .Any(t => t.DriverProfileId == d.Id
                               && t.Status != DeliveryStatus.Delivered
                               && t.Status != DeliveryStatus.Failed
                               && t.Status != DeliveryStatus.Cancelled))
                    .Where(d => !dbcontext.DriverTaskOffers
                        .Any(o => o.DeliveryTaskId == task.Id && o.DriverProfileId == d.Id))
                    .Select(d => new
                    {
                        d.Id,
                        DistanceMeters = d.CurrentLocation!.Distance(task.BranchLocation)
                    })
                    .OrderBy(d => d.DistanceMeters)
                    .FirstOrDefaultAsync(stoppingToken);

                if (nearest == null)
                    continue;

                // 4) إنشاء عرض محدود بوقت
                dbcontext.DriverTaskOffers.Add(new DriverTaskOffer
                {
                    Id = Guid.NewGuid(),
                    DeliveryTaskId = task.Id,
                    DriverProfileId = nearest.Id,
                    Status = DriverTaskOfferStatus.Pending,
                    MatchScore = (decimal)(nearest.DistanceMeters / 1000),
                    OfferedAt = now,
                    ExpiresAt = now.AddMinutes(OfferExpiryMinutes)
                });

                createdCount++;
            }

            if (createdCount > 0)
            {
                await dbcontext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("تم إرسال {Count} عرض توصيل للسائقين", createdCount);
            }
        }
       
    }
}