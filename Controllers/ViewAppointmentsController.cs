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
    // [Authorize(Roles = "customer")]
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
            var room = _context.Rooms.Include(r => r.Building).FirstOrDefault(r => r.Id == roomId);
            ViewBag.RoomId = roomId;
            ViewBag.Room = room;
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

        [Authorize(Roles = "host")]
        public async Task<IActionResult> HostRoomAppointments(Guid? roomId, string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.ViewAppointments
                .Include(a => a.Room)
                .Include(a => a.User)
                .Where(a => a.Room != null && a.Room.Building != null && a.Room.Building.HostId.ToString() == userId);
            if (roomId.HasValue)
                query = query.Where(a => a.RoomId == roomId);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ViewAppointmentStatus>(status, out var st))
                query = query.Where(a => a.Status == st);
            var appointments = await query
                .Include(a => a.User)
                .Include(a => a.Room)
                .ToListAsync();
            foreach (var appt in appointments)
            {
                if (appt.User?.ContactInformations != null)
                {
                    // Already loaded
                }
                if (appt.Room?.Building?.Host?.ContactInformations != null)
                {
                    // Already loaded
                }
            }
            ViewBag.Rooms = await _context.Rooms.Where(r => r.Building != null && r.Building.HostId.ToString() == userId).ToListAsync();
            ViewBag.SelectedRoomId = roomId;
            ViewBag.Status = status;
            return View("HostRoomAppointments", appointments);
        }

        [Authorize(Roles = "host")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(Guid id, string action)
        {
            var appt = await _context.ViewAppointments.Include(a => a.Room).ThenInclude(r => r.Building).FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null || appt.Room == null || appt.Room.Building == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appt.Room?.Building?.HostId.ToString() != userId) return Forbid();
            if (action == "accept")
                appt.Status = ViewAppointmentStatus.Confirmed;
            else if (action == "decline")
                appt.Status = ViewAppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();
            return RedirectToAction("HostRoomAppointments");
        }
    }
}
