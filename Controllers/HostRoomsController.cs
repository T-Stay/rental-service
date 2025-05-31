using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostRoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HostRoomsController> _logger;
        public HostRoomsController(AppDbContext context, ILogger<HostRoomsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /HostRooms?buildingId={buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var query = _context.Rooms.Include(r => r.Building)
                    .Where(r => r.Building != null && r.Building.HostId.ToString() == userId);
                if (buildingId.HasValue)
                    query = query.Where(r => r.BuildingId == buildingId);
                var rooms = await query.ToListAsync();
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = buildingId;
                return View(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rooms for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred loading your rooms. Please try again later.";
                return View(new List<Room>());
            }
        }

        // GET: /HostRooms/Create?buildingId={buildingId}
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = buildingId;
                ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
                ViewBag.SelectedAmenities = new List<Guid>();
                var room = new Room {
                    Name = string.Empty,
                    Description = string.Empty,
                    Images = new List<RoomImage>(),
                    Amenities = new List<Amenity>()
                };
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create room page for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred loading the create room page. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: /HostRooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            try
            {
                // Handle amenities
                var amenityIds = Request.Form["AmenityIds"].ToList();
                if (amenityIds.Any())
                {
                    room.Amenities = await _context.Amenities.Where(a => amenityIds.Contains(a.Id.ToString())).ToListAsync();
                }
                // TODO: Handle image upload and save RoomImages
                if (ModelState.IsValid)
                {
                    room.Id = Guid.NewGuid();
                    room.CreatedAt = room.UpdatedAt = DateTime.UtcNow;
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { buildingId = room.BuildingId });
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = room.BuildingId;
                ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
                ViewBag.SelectedAmenities = amenityIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => Guid.Parse(x!)).ToList();
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred while creating the room. Please try again later.";
                return View(room);
            }
        }

        // GET: /HostRooms/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var room = await _context.Rooms.Include(r => r.Amenities).Include(r => r.RoomImages).FirstOrDefaultAsync(r => r.Id == id);
                if (room == null) return NotFound();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = room.BuildingId;
                ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
                ViewBag.SelectedAmenities = room.Amenities?.Select(a => a.Id).ToList() ?? new List<Guid>();
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit room page for room {RoomId}", id);
                ViewBag.Error = "An error occurred loading the edit room page. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: /HostRooms/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Room room)
        {
            if (id != room.Id) return NotFound();
            try
            {
                // Handle amenities
                var amenityIds = Request.Form["AmenityIds"].ToList();
                if (amenityIds.Any())
                {
                    room.Amenities = await _context.Amenities.Where(a => amenityIds.Contains(a.Id.ToString())).ToListAsync();
                }
                // TODO: Handle image upload and save RoomImages
                if (ModelState.IsValid)
                {
                    room.UpdatedAt = DateTime.UtcNow;
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { buildingId = room.BuildingId });
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = room.BuildingId;
                ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
                ViewBag.SelectedAmenities = amenityIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => Guid.Parse(x!)).ToList();
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing room {RoomId}", id);
                ViewBag.Error = "An error occurred while editing the room. Please try again later.";
                return View(room);
            }
        }

        // GET: /HostRooms/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(id);
                if (room == null) return NotFound();
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room {RoomId}", id);
                ViewBag.Error = "An error occurred while deleting the room. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: /HostRooms/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(id);
                if (room != null)
                {
                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room {RoomId}", id);
                ViewBag.Error = "An error occurred while deleting the room. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /HostRooms/SetStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(Guid id, string status)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(id);
                if (room == null) return NotFound();
                if (Enum.TryParse<RoomStatus>(status, out var newStatus))
                {
                    room.Status = newStatus;
                    room.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index), new { buildingId = room?.BuildingId });
            }
            catch (Exception)
            {
                ViewBag.Error = "An error occurred while updating the room status. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /HostRooms/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(r => r.Images)
                    .Include(r => r.Amenities)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (room == null) return NotFound();
                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room details for room {RoomId}", id);
                ViewBag.Error = "An error occurred loading the room details. Please try again later.";
                return RedirectToAction("Index");
            }
        }
    }
}
