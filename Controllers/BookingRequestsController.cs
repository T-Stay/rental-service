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
        public async Task<IActionResult> Create(Guid? roomId)
        {
            if (roomId.HasValue)
            {
                ViewBag.RoomId = roomId.Value;
                return View();
            }
            // No roomId provided, show dropdown of available rooms
            var rooms = await _context.Rooms.Where(r => r.Status == RoomStatus.Active).ToListAsync();
            ViewBag.Rooms = rooms;
            return View();
        }

        // POST: /BookingRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid? roomId, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            if (!roomId.HasValue)
            {
                ModelState.AddModelError("roomId", "Room is required.");
                var rooms = await _context.Rooms.Where(r => r.Status == RoomStatus.Active).ToListAsync();
                ViewBag.Rooms = rooms;
                return View();
            }
            var request = new BookingRequest
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userId),
                RoomId = roomId.Value,
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
