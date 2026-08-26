using System.Security.Claims;
using coop.Dtos.AddressesController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/addresses")]
    [Authorize(Roles = "Customer")]
    public class AddressesController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public AddressesController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userId = GetCurrentUserId();

            var addresses = await _dbcontext.CustomerAddresses
                .Where(a => a.CustomerUserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressResponseDto
                {
                    Id = a.Id,
                    Label = a.Label,
                    ContactName = a.ContactName,
                    ContactPhone = a.ContactPhone,
                    City = a.City,
                    Area = a.Area,
                    Street = a.Street,
                    Building = a.Building,
                    Floor = a.Floor,
                    AdditionalDirections = a.AdditionalDirections,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    IsDefault = a.IsDefault,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(addresses);
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}