using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostBookingRequestsController : Controller
    {
        private readonly AppDbContext _context;
        public HostBookingRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /HostBookingRequests
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.User)
                .Where(b => b.Room != null && b.Room.Building != null && b.Room.Building.HostId.ToString() == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        // GET: /HostBookingRequests/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var request = await _context.BookingRequests
                .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (request == null || request.Room == null || request.Room.Building == null)
                return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (request.Room.Building.HostId.ToString() != userId)
                return Forbid();
            return View(request);
        }
    }
}
