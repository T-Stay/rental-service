using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Services;
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
        private readonly IEmailService _emailService;
        
        public HostBookingRequestsController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /HostBookingRequests
        public async Task<IActionResult> Index(string search, string status, string sort)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requestsQuery = _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.User)
                .Where(b => b.Room != null && b.Room.Building != null && b.Room.Building.HostId.ToString() == userId)
                .AsQueryable();

            // Filter by search (room name or customer name)
            if (!string.IsNullOrEmpty(search))
            {
                requestsQuery = requestsQuery.Where(b => (b.Room != null && b.Room.Name.Contains(search)) || (b.User != null && b.User.Name.Contains(search)));
            }
            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RentalService.Models.BookingRequestStatus>(status, out var st))
            {
                requestsQuery = requestsQuery.Where(b => b.Status == st);
            }
            // Sort
            switch (sort)
            {
                case "room_asc":
                    requestsQuery = requestsQuery.OrderBy(b => b.Room != null ? b.Room.Name : "");
                    break;
                case "room_desc":
                    requestsQuery = requestsQuery.OrderByDescending(b => b.Room != null ? b.Room.Name : "");
                    break;
                case "created_asc":
                    requestsQuery = requestsQuery.OrderBy(b => b.CreatedAt);
                    break;
                default:
                    requestsQuery = requestsQuery.OrderByDescending(b => b.CreatedAt);
                    break;
            }
            var requests = await requestsQuery.ToListAsync();
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
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
                    .ThenInclude(b => b.Host)
                    .ThenInclude(h => h.ContactInformations)
                    .Include(b => b.User)
                    .ThenInclude(u => u.ContactInformations)
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

                // Send email to customer
                if (request.User != null)
                {
                    var customerEmail = request.User.ContactInformations?
                        .FirstOrDefault(c => c.Type == RentalService.Models.ContactType.Email)?.Data;
                        
                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        // Prepare host contact info
                        string? hostContactInfo = null;
                        if (request.Room.Building.Host?.ContactInformations?.Any() == true)
                        {
                            var contacts = request.Room.Building.Host.ContactInformations
                                .Select(c => $"<p><strong>{c.Type}:</strong> {c.Data}</p>")
                                .ToList();
                            hostContactInfo = string.Join("", contacts);
                        }
                        
                        var detailsUrl = Url.Action("Details", "BookingRequests", new { id = request.Id }, Request.Scheme);
                        await _emailService.SendBookingRequestStatusUpdateAsync(
                            customerEmail,
                            request.User.Name,
                            request.Room.Name,
                            "Approved",
                            detailsUrl,
                            hostContactInfo);
                    }
                }
                
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
                    .Include(b => b.User)
                    .ThenInclude(u => u.ContactInformations)
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

                // Send email to customer
                if (request.User != null)
                {
                    var customerEmail = request.User.ContactInformations?
                        .FirstOrDefault(c => c.Type == RentalService.Models.ContactType.Email)?.Data;
                        
                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        var detailsUrl = Url.Action("Details", "BookingRequests", new { id = request.Id }, Request.Scheme);
                        await _emailService.SendBookingRequestStatusUpdateAsync(
                            customerEmail,
                            request.User.Name,
                            request.Room.Name,
                            "Rejected",
                            detailsUrl);
                    }
                }
                
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
