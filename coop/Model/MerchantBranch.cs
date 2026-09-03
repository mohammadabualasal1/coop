using coop.Enums;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace coop.Model
{
    public class MerchantBranch
    {
        public Guid Id { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [JsonIgnore]
        public Point? Location { get; set; }

        public string PhoneNumber { get; set; }
        public TimeOnly OpeningTime { get; set; }
        public TimeOnly ClosingTime { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinimumOrderAmount { get; set; }
        public decimal BaseDeliveryFee { get; set; }
        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}