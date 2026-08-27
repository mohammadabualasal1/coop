using coop.Enums;

namespace coop.Dtos.OrdersDtos
{
    public class PlaceOrderRequestDto
    {
        
            public Guid CustomerAddressId { get; set; }
            public PaymentMethod PaymentMethod { get; set; }
            public string? CustomerNotes { get; set; }
        
    }
}
