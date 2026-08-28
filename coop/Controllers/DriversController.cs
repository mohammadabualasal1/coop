using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Driver")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public DriversController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }









        private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
