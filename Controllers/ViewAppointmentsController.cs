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
        [Authorize(Roles = "customer")]
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

        // GET: /ViewAppointments/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var appt = await _context.ViewAppointments
                .Include(a => a.Room)
                .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFound();
            if (appt.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();
            // Eager load host and contact info if possible
            if (appt.Room?.Building != null)
            {
                await _context.Entry(appt.Room.Building)
                    .Reference(b => b.Host).LoadAsync();
                if (appt.Room.Building.Host != null)
                {
                    await _context.Entry(appt.Room.Building.Host)
                        .Collection(h => h.ContactInformations).LoadAsync();
                }
            }
            return View(appt);
        }

        // POST: /ViewAppointments/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var appt = await _context.ViewAppointments
                .Include(a => a.Room)
                .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId.ToString() == userId);
            if (appt == null)
            {
                TempData["ToastError"] = "Appointment not found.";
                return RedirectToAction("Index");
            }
            if (appt.Status != ViewAppointmentStatus.Pending)
            {
                TempData["ToastError"] = "Only pending appointments can be cancelled.";
                return RedirectToAction("Details", new { id });
            }
            appt.Status = ViewAppointmentStatus.Cancelled;
            // Notify host
            if (appt.Room?.Building != null)
            {
                await _context.Entry(appt.Room.Building).Reference(b => b.Host).LoadAsync();
                if (appt.Room.Building.Host != null)
                {
                    _context.Notifications.Add(new Notification {
                        Id = Guid.NewGuid(),
                        UserId = appt.Room.Building.Host.Id,
                        Title = "Appointment Cancelled",
                        Message = $"A viewing appointment for '{appt.Room?.Name}' has been cancelled by the customer.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();
            TempData["ToastSuccess"] = "Appointment cancelled.";
            return RedirectToAction("Details", new { id });
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
            // Eager load customer for notification
            await _context.Entry(appt).Reference(a => a.User).LoadAsync();
            if (action == "accept")
            {
                appt.Status = ViewAppointmentStatus.Confirmed;
                // Notify customer
                if (appt.User != null)
                {
                    _context.Notifications.Add(new Notification {
                        Id = Guid.NewGuid(),
                        UserId = appt.UserId,
                        Title = "Appointment Confirmed",
                        Message = $"Your viewing appointment for '{appt.Room?.Name}' has been confirmed by the host.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else if (action == "decline")
            {
                appt.Status = ViewAppointmentStatus.Cancelled;
                // Notify customer
                if (appt.User != null)
                {
                    _context.Notifications.Add(new Notification {
                        Id = Guid.NewGuid(),
                        UserId = appt.UserId,
                        Title = "Appointment Cancelled",
                        Message = $"Your viewing appointment for '{appt.Room?.Name}' has been cancelled by the host.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("HostRoomAppointments");
        }

        // GET: /ViewAppointments/HostDetails/{id}
        [Authorize(Roles = "host")]
        public async Task<IActionResult> HostDetails(Guid id)
        {
            var appt = await _context.ViewAppointments
                .Include(a => a.Room)
                .ThenInclude(r => r.Building)
                .ThenInclude(b => b.Host)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appt.Room?.Building?.HostId.ToString() != userId)
                return Forbid();
            // Eager load contact info
            if (appt.User != null)
                await _context.Entry(appt.User).Collection(u => u.ContactInformations).LoadAsync();
            if (appt.Room?.Building?.Host != null)
                await _context.Entry(appt.Room.Building.Host).Collection(h => h.ContactInformations).LoadAsync();
            return View("HostDetails", appt);
        }

        // GET: /ViewAppointments/HostRoomAppointments
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
    }
}
