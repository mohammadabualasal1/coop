using coop.Enums;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace coop.Model
{
    public class DriverProfile
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public VerificationStatus VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
        public bool IsAvailable { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }

        [JsonIgnore]
        public Point? CurrentLocation { get; set; }

        public DateTime? LastLocationAt { get; set; }
        public decimal? AverageRating { get; set; }
        public int CompletedDeliveries { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}