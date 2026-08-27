using coop.Enums;
using coop.Model;
using Microsoft.EntityFrameworkCore;

namespace coop.Services
{
    public class StockReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockReservationCleanupService> _logger;

        public StockReservationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<StockReservationCleanupService> logger)
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
                    await ReleaseExpiredReservationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء تحرير الحجوزات المنتهية");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ReleaseExpiredReservationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<CoopDbContext>();

            var now = DateTime.UtcNow;

            var expiredReservations = await dbcontext.StockReservations
                .Where(sr => sr.Status == StockReservationStatus.Active && sr.ExpiresAt <= now)
                .Include(sr => sr.BranchOffer)
                .Include(sr => sr.Order)
                .ToListAsync(stoppingToken);

            if (expiredReservations.Count == 0)
                return;

            using var transaction = await dbcontext.Database.BeginTransactionAsync(stoppingToken);

            try
            {
                var affectedOrders = new HashSet<Guid>();

                foreach (var reservation in expiredReservations)
                {
                    // الطلبات اللي التاجر قبلها ما بتتأثر، الحجز بيضل لحد التسليم
                    if (reservation.Order.Status != OrderStatus.PendingPayment &&
                        reservation.Order.Status != OrderStatus.PendingMerchantConfirmation)
                        continue;

                    reservation.BranchOffer.ReservedStock -= reservation.Quantity;
                    reservation.Status = StockReservationStatus.Expired;
                    reservation.ReleasedAt = now;

                    affectedOrders.Add(reservation.OrderId);
                }

                foreach (var orderId in affectedOrders)
                {
                    var order = expiredReservations.First(r => r.OrderId == orderId).Order;

                    var oldStatus = order.Status;
                    order.Status = OrderStatus.Cancelled;
                    order.CancellationReason = "انتهت مهلة الحجز";
                    order.UpdatedAt = now;

                    dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        OldStatus = oldStatus,
                        NewStatus = OrderStatus.Cancelled,
                        ChangedByUserId = order.CustomerUserId,
                        Note = "ألغي تلقائياً بعد انتهاء مهلة الحجز",
                        CreatedAt = now
                    });
                }

                await dbcontext.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);

                if (affectedOrders.Count > 0)
                    _logger.LogInformation("تم إلغاء {Count} طلب لانتهاء مهلة الحجز", affectedOrders.Count);
            }
            catch
            {
                await transaction.RollbackAsync(stoppingToken);
                throw;
            }
        }
    }
}