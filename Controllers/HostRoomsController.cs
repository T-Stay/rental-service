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
    [Authorize(Roles = "host")]
    public class HostRoomsController : Controller
    {
        private readonly AppDbContext _context;
        public HostRoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /HostRooms?buildingId={buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
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

        // GET: /HostRooms/Create?buildingId={buildingId}
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
            ViewBag.SelectedBuildingId = buildingId;
            ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
            ViewBag.SelectedAmenities = new List<Guid>();
            return View();
        }

        // POST: /HostRooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
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

        // GET: /HostRooms/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
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

        // POST: /HostRooms/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Room room)
        {
            if (id != room.Id) return NotFound();
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

        // GET: /HostRooms/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // POST: /HostRooms/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /HostRooms/SetStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(Guid id, string status)
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
    }
}
