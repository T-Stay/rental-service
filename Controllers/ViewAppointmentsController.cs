using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using RentalService.Services;
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
        private readonly IEmailService _emailService;
        
        public ViewAppointmentsController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /ViewAppointments
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> Index(string status, string sort)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.ViewAppointments
                .Where(a => a.UserId.ToString() == userId)
                .Include(a => a.Room)
                .AsQueryable();
            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RentalService.Models.ViewAppointmentStatus>(status, out var st))
            {
                query = query.Where(a => a.Status == st);
            }
            // Sort
            switch (sort)
            {
                case "date_asc":
                    query = query.OrderBy(a => a.AppointmentTime);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(a => a.AppointmentTime);
                    break;
                default:
                    query = query.OrderByDescending(a => a.CreatedAt);
                    break;
            }
            var appointments = await query.ToListAsync();
            ViewBag.Status = status;
            ViewBag.Sort = sort;
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
            
            // Get room and host info for email
            var room = await _context.Rooms
                .Include(r => r.Building)
                .ThenInclude(b => b.Host)
                .ThenInclude(h => h.ContactInformations)
                .FirstOrDefaultAsync(r => r.Id == roomId);
                
            if (room?.Building?.Host != null)
            {
                var hostEmail = room.Building.Host.ContactInformations?
                    .FirstOrDefault(c => c.Type == ContactType.Email)?.Data;
                    
                if (!string.IsNullOrEmpty(hostEmail))
                {
                    var detailsUrl = Url.Action("HostDetails", "ViewAppointments", new { id = appointment.Id }, Request.Scheme);
                    await _emailService.SendNewViewAppointmentNotificationAsync(
                        hostEmail, 
                        room.Building.Host.Name, 
                        room.Name, 
                        appointmentTime, 
                        detailsUrl);
                }
            }
            
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
            var appt = await _context.ViewAppointments
                .Include(a => a.Room)
                .ThenInclude(r => r.Building)
                .ThenInclude(b => b.Host)
                .ThenInclude(h => h.ContactInformations)
                .Include(a => a.User)
                .ThenInclude(u => u.ContactInformations)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (appt == null || appt.Room == null || appt.Room.Building == null) return NotFound();
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appt.Room?.Building?.HostId.ToString() != userId) return Forbid();

            string status = "";
            string? hostContactInfo = null;

            if (action == "accept")
            {
                appt.Status = ViewAppointmentStatus.Confirmed;
                status = "Confirmed";
                
                // Prepare host contact info for email
                if (appt.Room.Building.Host?.ContactInformations?.Any() == true)
                {
                    var contacts = appt.Room.Building.Host.ContactInformations
                        .Select(c => $"<p><strong>{c.Type}:</strong> {c.Data}</p>")
                        .ToList();
                    hostContactInfo = string.Join("", contacts);
                }
                
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
                status = "Cancelled";
                
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

            // Send email to customer
            if (appt.User != null && !string.IsNullOrEmpty(status))
            {
                var customerEmail = appt.User.ContactInformations?
                    .FirstOrDefault(c => c.Type == ContactType.Email)?.Data;
                    
                if (!string.IsNullOrEmpty(customerEmail))
                {
                    var detailsUrl = Url.Action("Details", "ViewAppointments", new { id = appt.Id }, Request.Scheme);
                    await _emailService.SendViewAppointmentStatusUpdateAsync(
                        customerEmail,
                        appt.User.Name,
                        appt.Room.Name,
                        status,
                        detailsUrl,
                        hostContactInfo);
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
        public async Task<IActionResult> HostRoomAppointments(Guid? buildingId, Guid? roomId, string status, string sort)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
            var rooms = new List<Room>();
            if (buildingId.HasValue)
            {
                rooms = await _context.Rooms.Where(r => r.BuildingId == buildingId.Value && r.Building != null && r.Building.HostId.ToString() == userId).ToListAsync();
            }
            else
            {
                rooms = await _context.Rooms.Where(r => r.Building != null && r.Building.HostId.ToString() == userId).ToListAsync();
            }
            var query = _context.ViewAppointments
                .Include(a => a.Room)
                .Include(a => a.User)
                .Where(a => a.Room != null && a.Room.Building != null && a.Room.Building.HostId.ToString() == userId);
            if (buildingId.HasValue)
                query = query.Where(a => a.Room != null && a.Room.BuildingId == buildingId.Value);
            if (roomId.HasValue)
                query = query.Where(a => a.RoomId == roomId);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RentalService.Models.ViewAppointmentStatus>(status, out var st))
                query = query.Where(a => a.Status == st);
            // Sort
            switch (sort)
            {
                case "date_asc":
                    query = query.OrderBy(a => a.AppointmentTime);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(a => a.AppointmentTime);
                    break;
                default:
                    query = query.OrderByDescending(a => a.CreatedAt);
                    break;
            }
            var appointments = await query
                .Include(a => a.User)
                .Include(a => a.Room)
                .ToListAsync();
            ViewBag.Buildings = buildings;
            ViewBag.SelectedBuildingId = buildingId;
            ViewBag.Rooms = rooms;
            ViewBag.SelectedRoomId = roomId;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            return View("HostRoomAppointments", appointments);
        }
    }
}
