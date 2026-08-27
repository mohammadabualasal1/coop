namespace coop.Dtos.MerchantOrdersDtos
{
    public class MerchantOrderItemResponseDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal DiscountedUnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
