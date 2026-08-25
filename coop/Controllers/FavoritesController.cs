using coop.Dtos.FavoritesController;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    [Authorize(Roles = "Customer")]
    public class FavoritesController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public FavoritesController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = GetCurrentUserId();

            var favorites = await _dbcontext.FavoriteOffers
                .Where(f => f.CustomerUserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FavoriteOfferResponse
                {
                    Id = f.Id,
                    OfferId = f.OfferId,
                    Title = f.Offer.Title,
                    DiscountedPrice = f.Offer.DiscountedPrice,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(favorites);
        }
        [HttpPost("{offerId}")]
        public async Task<IActionResult> AddFavorite(Guid offerId)
        {
            var userId = GetCurrentUserId();

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.EndAt < DateTime.UtcNow)
                return BadRequest("لا يمكن إضافة عرض منتهٍ للمفضلة");

            var alreadyFavorited = await _dbcontext.FavoriteOffers
                .AnyAsync(f => f.CustomerUserId == userId && f.OfferId == offerId);

            if (alreadyFavorited)
                return BadRequest("العرض موجود بالفعل في المفضلة");

            var favorite = new FavoriteOffer
            {
                Id = Guid.NewGuid(),
                CustomerUserId = userId,
                OfferId = offerId,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.FavoriteOffers.Add(favorite);
            await _dbcontext.SaveChangesAsync();

            return Ok(new FavoriteOfferResponse
            {
                Id = favorite.Id,
                OfferId = favorite.OfferId,
                Title = offer.Title,
                DiscountedPrice = offer.DiscountedPrice,
                CreatedAt = favorite.CreatedAt
            });
        }












        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}