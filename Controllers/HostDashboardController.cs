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
                ViewBag.Buildings = buildings;
                ViewBag.Rooms = rooms;
                ViewBag.Appointments = appointments;
                ViewBag.BookingRequests = bookingRequests;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading host dashboard for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred loading your dashboard. Please try again later.";
                return View();
            }
        }
    }
}
