using System.Security.Claims;
using coop.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace coop.Hubs
{
    [Authorize]
    public class TrackingHub : Hub
    {
        private readonly CoopDbContext _dbcontext;

        public TrackingHub(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task JoinOrderGroup(Guid orderId)
        {
            var userId = GetCurrentUserId();

            var canAccess = await CanAccessOrderAsync(orderId, userId);
            if (!canAccess)
                throw new HubException("لا تملك صلاحية متابعة هذا الطلب");

            await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
        }

        public async Task LeaveOrderGroup(Guid orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroup(orderId));
        }

        private async Task<bool> CanAccessOrderAsync(Guid orderId, Guid userId)
        {
            var order = await _dbcontext.Orders
                .Where(o => o.Id == orderId)
                .Select(o => new { o.CustomerUserId, o.MerchantId })
                .FirstOrDefaultAsync();

            if (order == null)
                return false;

            // الزبون صاحب الطلب
            if (order.CustomerUserId == userId)
                return true;

            // التاجر صاحب الطلب
            var isMerchantOwner = await _dbcontext.Merchants
                .AnyAsync(m => m.Id == order.MerchantId && m.OwnerUserId == userId);
            if (isMerchantOwner)
                return true;

            // السائق المكلّف بالمهمة
            var isAssignedDriver = await _dbcontext.DeliveryTasks
                .AnyAsync(t => t.OrderId == orderId
                            && t.DriverProfile != null
                            && t.DriverProfile.UserId == userId);
            if (isAssignedDriver)
                return true;

            // الأدمن
            return Context.User?.IsInRole(UserRole.Admin.ToString()) == true;
        }

        public static string OrderGroup(Guid orderId) => $"order-{orderId}";

        private Guid GetCurrentUserId() =>
            Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}