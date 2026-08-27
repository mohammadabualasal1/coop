namespace coop.Dtos.OrdersDtos
{
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public string ProductNameSnapshot { get; set; }
        public decimal OriginalUnitPrice { get; set; }
        public decimal DiscountedUnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
