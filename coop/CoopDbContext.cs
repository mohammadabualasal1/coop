using Microsoft.EntityFrameworkCore;
using coop.Model;

namespace coop
{
    public class CoopDbContext : DbContext
    {
       
            public CoopDbContext(DbContextOptions<CoopDbContext> options)
                : base(options)
            {

            }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<MerchantBranch> MerchantBranches { get; set; }
        public DbSet<VerificationDocument> VerificationDocuments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<BranchOffer> BranchOffers { get; set; }
        public DbSet<FavoriteOffer> FavoriteOffers { get; set; }
        public DbSet<FollowedMerchant> FollowedMerchants { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<DriverAvailability> DriverAvailabilities { get; set; }
        public DbSet<DeliveryTask> DeliveryTasks { get; set; }
        public DbSet<DriverTaskOffer> DriverTaskOffers { get; set; }
        public DbSet<DriverLocation> DriverLocations { get; set; }
        public DbSet<ConfirmationToken> ConfirmationTokens { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }
    }
}