using coop.Dtos.AddressesController;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using coop.Model;
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
        [HttpPost]
        public async Task<IActionResult> CreateAddress(CreateAddressRequestDto dto)
        {
            var userId = GetCurrentUserId();

            if (dto.Latitude < -90 || dto.Latitude > 90 ||
                dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("الإحداثيات غير صحيحة");

            var isFirstAddress = !await _dbcontext.CustomerAddresses
                .AnyAsync(a => a.CustomerUserId == userId);

            var address = new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerUserId = userId,
                Label = dto.Label,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                City = dto.City,
                Area = dto.Area,
                Street = dto.Street,
                Building = dto.Building,
                Floor = dto.Floor,
                AdditionalDirections = dto.AdditionalDirections,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsDefault = isFirstAddress,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.CustomerAddresses.Add(address);
            await _dbcontext.SaveChangesAsync();

            return Ok(new AddressResponseDto
            {
                Id = address.Id,
                Label = address.Label,
                ContactName = address.ContactName,
                ContactPhone = address.ContactPhone,
                City = address.City,
                Area = address.Area,
                Street = address.Street,
                Building = address.Building,
                Floor = address.Floor,
                AdditionalDirections = address.AdditionalDirections,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}