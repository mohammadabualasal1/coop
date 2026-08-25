using System.Security.Claims;
using coop.Dtos.FavoritesController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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













        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}