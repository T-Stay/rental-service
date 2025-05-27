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
    public class ViewAppointmentsController : Controller
    {
        private readonly AppDbContext _context;
        public ViewAppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /ViewAppointments
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointments = await _context.ViewAppointments
                .Where(a => a.UserId.ToString() == userId)
                .Include(a => a.Room)
                .ToListAsync();
            return View(appointments);
        }

        // GET: /ViewAppointments/Create/{roomId}
        public IActionResult Create(Guid roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        // POST: /ViewAppointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid roomId, DateTime appointmentTime)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var appointment = new ViewAppointment
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userId),
                RoomId = roomId,
                AppointmentTime = appointmentTime,
                Status = ViewAppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.ViewAppointments.Add(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
