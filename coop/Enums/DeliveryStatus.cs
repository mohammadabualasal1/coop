// DeliveryStatus.cs
namespace coop.Enums
{
    public enum DeliveryStatus
    {
        SearchingDriver,
        Offered,
        Assigned,
        GoingToMerchant,
        ArrivedAtMerchant,
        PickedUp,
        GoingToCustomer,
        ArrivedAtCustomer,
        Delivered,
        Failed,
        Cancelled,
    }
}