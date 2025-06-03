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
    }
}
