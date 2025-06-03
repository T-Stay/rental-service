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
            // Notify host
            var room = await _context.Rooms.Include(r => r.Building).FirstOrDefaultAsync(r => r.Id == roomId.Value);
            if (room?.Building != null)
            {
                _context.Notifications.Add(new Notification {
                    Id = Guid.NewGuid(),
                    UserId = room.Building.HostId,
                    Title = "New Booking Request",
                    Message = $"A new booking request for '{room.Name}' has been submitted.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /BookingRequests/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var request = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (request?.Room != null)
            {
                await _context.Entry(request.Room)
                    .Reference(r => r.Building).LoadAsync();
                if (request.Room.Building != null)
                {
                    await _context.Entry(request.Room.Building)
                        .Reference(b => b.Host).LoadAsync();
                    if (request.Room.Building.Host != null)
                    {
                        await _context.Entry(request.Room.Building.Host)
                            .Collection(h => h.ContactInformations).LoadAsync();
                    }
                }
            }
            if (request?.User != null)
            {
                await _context.Entry(request.User)
                    .Collection(u => u.ContactInformations).LoadAsync();
            }
            if (request?.Room?.Building?.Host?.ContactInformations != null)
            {
                // Already loaded
            }
            if (request?.User?.ContactInformations != null)
            {
                // Already loaded
            }
            if (request == null) return NotFound();
            return View(request);
        }

        // POST: /BookingRequests/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var request = await _context.BookingRequests
                .Include(b => b.Room)
                .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId.ToString() == userId);
            if (request == null)
            {
                TempData["ToastError"] = "Booking request not found.";
                return RedirectToAction("Index");
            }
            if (request.Status != BookingRequestStatus.Pending && request.Status != BookingRequestStatus.Approved)
            {
                TempData["ToastError"] = "Only pending or approved requests can be cancelled.";
                return RedirectToAction("Details", new { id });
            }
            request.Status = BookingRequestStatus.Cancelled;
            request.UpdatedAt = DateTime.UtcNow;
            // Notify host
            if (request.Room?.Building != null)
            {
                _context.Notifications.Add(new Notification {
                    Id = Guid.NewGuid(),
                    UserId = request.Room.Building.HostId,
                    Title = "Booking Cancelled",
                    Message = $"A booking request for '{request.Room?.Name}' has been cancelled by the customer.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = "Booking request cancelled.";
            return RedirectToAction("Details", new { id });
        }
    }
}
