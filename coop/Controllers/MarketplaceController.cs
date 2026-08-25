using coop.Dtos.MarketplaceController;
using coop.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/marketplace")]
    public class MarketplaceController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

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

            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(request.Latitude * Math.PI / 180));

            var minLat = request.Latitude - latDelta;
            var maxLat = request.Latitude + latDelta;
            var minLng = request.Longitude - lngDelta;
            var maxLng = request.Longitude + lngDelta;

            var candidates = await _dbcontext.BranchOffers
                .Where(bo => bo.IsAvailable
                          && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0
                          && bo.Offer.Status == OfferStatus.Active
                          && bo.Offer.StartAt <= now
                          && bo.Offer.EndAt >= now
                          && bo.MerchantBranch.IsActive
                          && bo.MerchantBranch.Latitude >= minLat
                          && bo.MerchantBranch.Latitude <= maxLat
                          && bo.MerchantBranch.Longitude >= minLng
                          && bo.MerchantBranch.Longitude <= maxLng)
                .Select(bo => new
                {
                    BranchLatitude = bo.MerchantBranch.Latitude,
                    BranchLongitude = bo.MerchantBranch.Longitude,
                    Offer = new OfferSummaryResponse
                    {
                        Id = bo.Offer.Id,
                        Title = bo.Offer.Title,
                        MerchantId = bo.Offer.MerchantId,
                        MerchantName = bo.Offer.Merchant.Name,
                        MainImageUrl = bo.Offer.Product.MainImageUrl,
                        OriginalPrice = bo.Offer.OriginalPrice,
                        DiscountedPrice = bo.Offer.DiscountedPrice,
                        DiscountPercentage = bo.Offer.DiscountPercentage,
                        EndAt = bo.Offer.EndAt,
                        DistanceKm = null
                    }
                })
                .ToListAsync();

            var offers = candidates
                .Select(c =>
                {
                    c.Offer.DistanceKm = CalculateDistanceKm(
                        request.Latitude, request.Longitude,
                        c.BranchLatitude, c.BranchLongitude);
                    return c.Offer;
                })
                .Where(o => o.DistanceKm <= radiusKm)
                .GroupBy(o => o.Id)
                .Select(g => g.OrderBy(o => o.DistanceKm).First())
                .OrderBy(o => o.DistanceKm)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(offers);
        }

        private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371;

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
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

    }
}