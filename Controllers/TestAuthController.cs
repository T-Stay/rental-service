using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RentalService.Controllers
{
    [Authorize]
    public class TestAuthController : Controller
    {
        [Authorize(Roles = "admin")]
        public IActionResult AdminOnly()
        {
            return Content("You are an admin!");
        }

        [Authorize(Roles = "host")]
        public IActionResult HostOnly()
        {
            return Content("You are a host!");
        }

        [Authorize(Roles = "customer")]
        public IActionResult CustomerOnly()
        {
            return Content("You are a customer!");
        }

        [AllowAnonymous]
        public IActionResult Anyone()
        {
            return Content("Anyone can access this.");
        }
    }
}
