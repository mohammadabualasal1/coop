using coop.Enums;

namespace coop.Dtos.CheckoutController
{
    public class CreateOrderRequestDto
    {
        public Guid CustomerAddressId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? CustomerNotes { get; set; }
    }
}
