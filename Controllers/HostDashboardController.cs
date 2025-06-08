using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalService.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HostDashboardController> _logger;
        public HostDashboardController(AppDbContext context, ILogger<HostDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /HostDashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // Check contact info count
                var contactInfoCount = await _context.ContactInformations.CountAsync(c => c.UserId.ToString() == userId);
                ViewBag.ContactInfoIncomplete = contactInfoCount < 2;
                var buildings = await _context.Buildings
                    .Where(b => b.HostId.ToString() == userId)
                    .Include(b => b.Rooms)
                    .ToListAsync();
                var rooms = await _context.Rooms
                    .Where(r => r.Building != null && r.Building.HostId.ToString() == userId)
                    .Include(r => r.Building)
                    .ToListAsync();
                var appointments = await _context.ViewAppointments
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .Where(a => a.Room != null && a.Room.Building != null && a.Room.Building.HostId.ToString() == userId)
                    .OrderByDescending(a => a.AppointmentTime)
                    .Take(5)
                    .ToListAsync();
                var bookingRequests = await _context.BookingRequests
                    .Include(br => br.Room)
                    .Where(br => br.Room != null && br.Room.Building != null && br.Room.Building.HostId.ToString() == userId)
                    .OrderByDescending(br => br.CreatedAt)
                    .Take(5)
                    .ToListAsync();
                var notifications = await _context.Notifications
                    .Where(n => n.UserId.ToString() == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
                // Thêm thống kê số lượng quảng cáo của chủ trọ
                var myAdPostsCount = await _context.AdPosts.CountAsync(a => a.HostId == userId);
                ViewBag.MyAdPostsCount = myAdPostsCount;
                ViewBag.Buildings = buildings;
                ViewBag.Rooms = rooms;
                ViewBag.Appointments = appointments;
                ViewBag.BookingRequests = bookingRequests;
                ViewBag.Notifications = notifications;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading host dashboard for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred loading your dashboard. Please try again later.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId.ToString() == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
