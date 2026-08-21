// OrderStatus.cs
namespace coop.Enums
{
    public enum OrderStatus
    {
        PendingPayment,
        PendingMerchantConfirmation,
        Accepted,
        Rejected,
        Preparing,
        ReadyForPickup,
        DriverAssigned,
        OutForDelivery,
        Delivered,
        Completed,
        Cancelled,
        DeliveryFailed,
    }
}