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
                .Where(t => !dbcontext.DriverTaskOffers
                    .Any(o => o.DeliveryTaskId == t.Id && o.Status == DriverTaskOfferStatus.Pending))
                .Select(t => new
                {
                    t.Id,
                    BranchLatitude = t.PickupBranch.Latitude,
                    BranchLongitude = t.PickupBranch.Longitude
                })
                .ToListAsync(stoppingToken);

            if (tasks.Count == 0)
                return;

            var createdCount = 0;

            foreach (var task in tasks)
            {
                // 3) السائقون المرشحون
                var candidates = await dbcontext.DriverProfiles
                    .Where(d => d.VerificationStatus == VerificationStatus.Approved)
                    .Where(d => d.IsAvailable)
                    .Where(d => d.CurrentLatitude != null && d.CurrentLongitude != null)
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
                        Latitude = d.CurrentLatitude!.Value,
                        Longitude = d.CurrentLongitude!.Value
                    })
                    .ToListAsync(stoppingToken);

                if (candidates.Count == 0)
                    continue;

                // 4) أقرب سائق للفرع
                var nearest = candidates
                    .Select(c => new
                    {
                        c.Id,
                        Distance = CalculateDistanceKm(
                            task.BranchLatitude, task.BranchLongitude,
                            c.Latitude, c.Longitude)
                    })
                    .OrderBy(c => c.Distance)
                    .First();

                // 5) إنشاء عرض محدود بوقت
                dbcontext.DriverTaskOffers.Add(new DriverTaskOffer
                {
                    Id = Guid.NewGuid(),
                    DeliveryTaskId = task.Id,
                    DriverProfileId = nearest.Id,
                    Status = DriverTaskOfferStatus.Pending,
                    MatchScore = (decimal)nearest.Distance,
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

        private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371;

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }
    }
}