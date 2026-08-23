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
    }
}