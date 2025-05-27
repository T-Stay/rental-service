using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    [Authorize(Roles = "customer")]
    public class BookingRequestsController : Controller
    {
        private readonly AppDbContext _context;
        public BookingRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /BookingRequests
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.BookingRequests
                .Where(b => b.UserId.ToString() == userId)
                .Include(b => b.Room)
                .ToListAsync();
            return View(requests);
        }

        // GET: /BookingRequests/Create/{roomId}
        public IActionResult Create(Guid roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        // POST: /BookingRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid roomId, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = new BookingRequest
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userId),
                RoomId = roomId,
                Message = message,
                Status = BookingRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /BookingRequests/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var request = await _context.BookingRequests.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
            if (request == null) return NotFound();
            return View(request);
        }
    }
}
