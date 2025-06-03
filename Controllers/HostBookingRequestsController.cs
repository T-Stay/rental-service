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
            if (request == null || request.Room == null || request.Room.Building == null)
                return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (request.Room.Building.HostId.ToString() != userId)
                return Forbid();
            return View(request);
        }

        // POST: /HostBookingRequests/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var request = await _context.BookingRequests
                    .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (request == null || request.Room == null || request.Room.Building == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Booking request not found." });
                    TempData["ToastError"] = "Booking request not found.";
                    return RedirectToAction("Details", new { id });
                }
                if (request.Room.Building.HostId.ToString() != userId)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Unauthorized." });
                    TempData["ToastError"] = "Unauthorized.";
                    return RedirectToAction("Details", new { id });
                }
                if (request.Status != RentalService.Models.BookingRequestStatus.Pending)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Only pending requests can be approved." });
                    TempData["ToastError"] = "Only pending requests can be approved.";
                    return RedirectToAction("Details", new { id });
                }
                request.Status = RentalService.Models.BookingRequestStatus.Approved;
                request.UpdatedAt = DateTime.UtcNow;
                // Create notification for customer
                _context.Notifications.Add(new RentalService.Models.Notification {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Title = "Booking Approved",
                    Message = $"Your booking request for '{request.Room?.Name}' has been approved.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Booking request approved." });
                TempData["ToastSuccess"] = "Booking request approved.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                TempData["ToastError"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: /HostBookingRequests/Reject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var request = await _context.BookingRequests
                    .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (request == null || request.Room == null || request.Room.Building == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Booking request not found." });
                    TempData["ToastError"] = "Booking request not found.";
                    return RedirectToAction("Details", new { id });
                }
                if (request.Room.Building.HostId.ToString() != userId)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Unauthorized." });
                    TempData["ToastError"] = "Unauthorized.";
                    return RedirectToAction("Details", new { id });
                }
                if (request.Status != RentalService.Models.BookingRequestStatus.Pending)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Only pending requests can be rejected." });
                    TempData["ToastError"] = "Only pending requests can be rejected.";
                    return RedirectToAction("Details", new { id });
                }
                request.Status = RentalService.Models.BookingRequestStatus.Rejected;
                request.UpdatedAt = DateTime.UtcNow;
                // Create notification for customer
                _context.Notifications.Add(new RentalService.Models.Notification {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Title = "Booking Rejected",
                    Message = $"Your booking request for '{request.Room?.Name}' has been rejected.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Booking request rejected." });
                TempData["ToastSuccess"] = "Booking request rejected.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                TempData["ToastError"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // If you have a Cancel action for host or customer, add notification logic like this:
        // For host cancelling a booking (notify customer):
        // request.Status = BookingRequestStatus.Cancelled;
        // _context.Notifications.Add(new Notification {
        //     Id = Guid.NewGuid(),
        //     UserId = request.UserId,
        //     Title = "Booking Cancelled",
        //     Message = $"Your booking request for '{request.Room?.Name}' has been cancelled by the host.",
        //     IsRead = false,
        //     CreatedAt = DateTime.UtcNow
        // });
        // For customer cancelling a booking (notify host):
        // _context.Notifications.Add(new Notification {
        //     Id = Guid.NewGuid(),
        //     UserId = request.Room.Building.HostId,
        //     Title = "Booking Cancelled",
        //     Message = $"A booking request for '{request.Room?.Name}' has been cancelled by the customer.",
        //     IsRead = false,
        //     CreatedAt = DateTime.UtcNow
        // });
    }
}
