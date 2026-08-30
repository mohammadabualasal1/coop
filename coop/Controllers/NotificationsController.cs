using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public NotificationsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
    }
}
