using coop.Dtos.MarketplaceController;
using coop.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/marketplace")]
    public class MarketplaceController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public MarketplaceController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet("offers")]
        public async Task<IActionResult> SearchOffers([FromQuery] OfferSearchRequestDto request)
        {
            var now = DateTime.UtcNow;

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 || request.PageSize > 100 ? 20 : request.PageSize;

            var query = _dbcontext.Offers
                .Where(o => o.Status == OfferStatus.Active)
                .Where(o => o.StartAt <= now && o.EndAt >= now)
                .Where(o => _dbcontext.BranchOffers.Any(bo => bo.OfferId == o.Id
                                                           && bo.IsAvailable
                                                           && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(o => o.Title.ToLower().Contains(search)
                                      || o.Product.Name.ToLower().Contains(search)
                                      || o.Merchant.Name.ToLower().Contains(search));
            }

            if (request.CategoryId != null)
                query = query.Where(o => o.Product.CategoryId == request.CategoryId);

            if (request.MerchantId != null)
                query = query.Where(o => o.MerchantId == request.MerchantId);

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim().ToLower();
                query = query.Where(o => _dbcontext.BranchOffers
                    .Any(bo => bo.OfferId == o.Id && bo.MerchantBranch.City.ToLower() == city));
            }

            if (request.MinimumDiscount != null)
                query = query.Where(o => o.DiscountPercentage >= request.MinimumDiscount);

            if (request.MinPrice != null)
                query = query.Where(o => o.DiscountedPrice >= request.MinPrice);

            if (request.MaxPrice != null)
                query = query.Where(o => o.DiscountedPrice <= request.MaxPrice);

            query = request.SortBy switch
            {
                "priceAsc" => query.OrderBy(o => o.DiscountedPrice),
                "priceDesc" => query.OrderByDescending(o => o.DiscountedPrice),
                "endingSoon" => query.OrderBy(o => o.EndAt),
                "newest" => query.OrderByDescending(o => o.CreatedAt),
                _ => query.OrderByDescending(o => o.DiscountPercentage)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            return Ok(new PagedResponse<OfferSummaryResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        [HttpGet("offers/nearby")]
        public async Task<IActionResult> GetNearbyOffers([FromQuery] NearbyOffersRequestDto request)
        {
            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
                return BadRequest("الإحداثيات غير صحيحة");

            var now = DateTime.UtcNow;

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 || request.PageSize > 100 ? 20 : request.PageSize;
            var radiusKm = request.RadiusKm <= 0 || request.RadiusKm > 100 ? 10 : request.RadiusKm;

            var origin = new Point(request.Longitude, request.Latitude) { SRID = 4326 };
            var radiusMeters = radiusKm * 1000;

            var nearest = await _dbcontext.BranchOffers
                .Where(bo => bo.IsAvailable
                          && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0
                          && bo.Offer.Status == OfferStatus.Active
                          && bo.Offer.StartAt <= now
                          && bo.Offer.EndAt >= now
                          && bo.MerchantBranch.IsActive
                          && bo.MerchantBranch.Location != null
                          && bo.MerchantBranch.Location.IsWithinDistance(origin, radiusMeters))
                .GroupBy(bo => bo.OfferId)
                .Select(g => new
                {
                    OfferId = g.Key,
                    DistanceMeters = g.Min(x => x.MerchantBranch.Location!.Distance(origin))
                })
                .OrderBy(x => x.DistanceMeters)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (nearest.Count == 0)
                return Ok(new List<OfferSummaryResponse>());

            var offerIds = nearest.Select(n => n.OfferId).ToList();

            var offers = await _dbcontext.Offers
                .Where(o => offerIds.Contains(o.Id))
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            var result = nearest
                .Select(n =>
                {
                    var offer = offers.First(o => o.Id == n.OfferId);
                    offer.DistanceKm = n.DistanceMeters / 1000;
                    return offer;
                })
                .ToList();

            return Ok(result);
        }
       
        [HttpGet("offers/ending-soon")]
        public async Task<IActionResult> GetEndingSoonOffers([FromQuery] int hoursWindow = 48, [FromQuery] int limit = 20)
        {
            var now = DateTime.UtcNow;

            if (hoursWindow < 1 || hoursWindow > 720)
                hoursWindow = 48;

            if (limit < 1 || limit > 100)
                limit = 20;

            var windowEnd = now.AddHours(hoursWindow);

            var offers = await _dbcontext.Offers
                .Where(o => o.Status == OfferStatus.Active)
                .Where(o => o.StartAt <= now && o.EndAt >= now)
                .Where(o => o.EndAt <= windowEnd)
                .Where(o => _dbcontext.BranchOffers.Any(bo => bo.OfferId == o.Id
                                                           && bo.IsAvailable
                                                           && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0))
                .OrderBy(o => o.EndAt)
                .Take(limit)
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            return Ok(offers);
        }
        [HttpGet("offers/top-discounts")]
        public async Task<IActionResult> GetTopDiscountOffers([FromQuery] int limit = 20)
        {
            var now = DateTime.UtcNow;

            if (limit < 1 || limit > 100)
                limit = 20;

            var offers = await _dbcontext.Offers
                .Where(o => o.Status == OfferStatus.Active)
                .Where(o => o.StartAt <= now && o.EndAt >= now)
                .Where(o => _dbcontext.BranchOffers.Any(bo => bo.OfferId == o.Id
                                                           && bo.IsAvailable
                                                           && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0))
                .OrderByDescending(o => o.DiscountPercentage)
                .Take(limit)
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            return Ok(offers);
        }
        [HttpGet("offers/{id}")]
        public async Task<IActionResult> GetOfferById(Guid id)
        {
            var now = DateTime.UtcNow;

            var offer = await _dbcontext.Offers
                .Where(o => o.Id == id)
                .Where(o => o.Status == OfferStatus.Active)
                .Where(o => o.StartAt <= now && o.EndAt >= now)
                .Select(o => new OfferDetailResponseDto
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    ProductId = o.ProductId,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    Status = o.Status,
                    MaximumQuantityPerCustomer = o.MaximumQuantityPerCustomer,
                    Branches = _dbcontext.BranchOffers
                        .Where(bo => bo.OfferId == o.Id && bo.MerchantBranch.IsActive)
                        .Select(bo => new BranchStockResponse
                        {
                            MerchantBranchId = bo.MerchantBranchId,
                            BranchName = bo.MerchantBranch.Name,
                            City = bo.MerchantBranch.City,
                            TotalStock = bo.TotalStock,
                            AvailableStock = bo.TotalStock - bo.ReservedStock - bo.SoldStock,
                            IsAvailable = bo.IsAvailable && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (offer == null)
                return NotFound("العرض غير موجود أو غير متاح");

            return Ok(offer);
        }
        [HttpGet("merchants")]
        public async Task<IActionResult> SearchMerchants([FromQuery] MerchantSearchRequestDto request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 || request.PageSize > 100 ? 20 : request.PageSize;

            var query = _dbcontext.Merchants
                .Where(m => m.IsActive)
                .Where(m => m.VerificationStatus == VerificationStatus.Approved);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim().ToLower();
                query = query.Where(m => _dbcontext.MerchantBranches
                    .Any(b => b.MerchantId == m.Id && b.IsActive && b.City.ToLower() == city));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.AverageRating)
                .ThenBy(m => m.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MerchantSummaryResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    LogoUrl = m.LogoUrl,
                    AverageRating = m.AverageRating
                })
                .ToListAsync();

            return Ok(new PagedResponse<MerchantSummaryResponseDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        [HttpGet("merchants/{id}")]
        public async Task<IActionResult> GetMerchantById(Guid id)
        {
            var merchant = await _dbcontext.Merchants
                .Where(m => m.Id == id)
                .Where(m => m.IsActive)
                .Where(m => m.VerificationStatus == VerificationStatus.Approved)
                .Select(m => new MerchantDetailResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    LogoUrl = m.LogoUrl,
                    CoverImageUrl = m.CoverImageUrl,
                    ContactEmail = m.ContactEmail,
                    ContactPhone = m.ContactPhone,
                    AverageRating = m.AverageRating,
                    Branches = _dbcontext.MerchantBranches
                        .Where(b => b.MerchantId == m.Id && b.IsActive)
                        .OrderByDescending(b => b.IsMainBranch)
                        .ThenBy(b => b.Name)
                        .Select(b => new MerchantBranchSummaryResponseDto
                        {
                            Id = b.Id,
                            Name = b.Name,
                            City = b.City,
                            Area = b.Area,
                            Latitude = b.Latitude,
                            Longitude = b.Longitude
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (merchant == null)
                return NotFound("التاجر غير موجود أو غير متاح");

            return Ok(merchant);
        }
        [HttpGet("merchants/{id}/offers")]
        public async Task<IActionResult> GetMerchantOffers(Guid id, [FromQuery] int limit = 50)
        {
            var now = DateTime.UtcNow;

            if (limit < 1 || limit > 100)
                limit = 50;

            var merchantExists = await _dbcontext.Merchants
                .AnyAsync(m => m.Id == id && m.IsActive && m.VerificationStatus == VerificationStatus.Approved);

            if (!merchantExists)
                return NotFound("التاجر غير موجود أو غير متاح");

            var offers = await _dbcontext.Offers
                .Where(o => o.MerchantId == id)
                .Where(o => o.Status == OfferStatus.Active)
                .Where(o => o.StartAt <= now && o.EndAt >= now)
                .Where(o => _dbcontext.BranchOffers.Any(bo => bo.OfferId == o.Id
                                                           && bo.IsAvailable
                                                           && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0))
                .OrderByDescending(o => o.DiscountPercentage)
                .Take(limit)
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            return Ok(offers);
        }
    }
}