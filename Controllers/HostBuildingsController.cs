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
    public class HostBuildingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HostBuildingsController> _logger;
        public HostBuildingsController(AppDbContext context, ILogger<HostBuildingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /HostBuildings
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var buildings = await _context.Buildings
                    .Where(b => b.HostId.ToString() == userId)
                    .Include(b => b.Rooms)
                    .ToListAsync();
                return View(buildings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading buildings for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred loading your buildings. Please try again later.";
                return View(new List<Building>());
            }
        }

        // GET: /HostBuildings/Create
        public IActionResult Create() => View();

        // POST: /HostBuildings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Building building)
        {
            try
            {
                // log the incoming building data
                Console.WriteLine($"Creating building: {building.Name}, Address: {building.Address}, Description: {building.Description}, Location: {building.Location}");
                if (ModelState.IsValid)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
                        ViewBag.Error = "User not found.";
                        return View(building);
                    }
                    building.Id = Guid.NewGuid();
                    building.HostId = Guid.Parse(userId);
                    building.CreatedAt = building.UpdatedAt = DateTime.UtcNow;
                    _context.Buildings.Add(building);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else 
                {
                    // log the model state errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Model error: {error.ErrorMessage}");
                    }
                }
                return View(building);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating building for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Error = "An error occurred while creating the building. Please try again later.";
                return View(building);
            }
        }

        // GET: /HostBuildings/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var building = await _context.Buildings.FindAsync(id);
                if (building == null) return NotFound();
                return View(building);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading building edit for building {BuildingId}", id);
                ViewBag.Error = "An error occurred loading the building. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: /HostBuildings/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Building building)
        {
            if (id != building.Id) return NotFound();
            try
            {
                if (ModelState.IsValid)
                {
                    var existing = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existing == null) return NotFound();
                    // Giữ nguyên HostId, CreatedAt
                    building.HostId = existing.HostId;
                    building.CreatedAt = existing.CreatedAt;
                    building.UpdatedAt = DateTime.UtcNow;
                    _context.Entry(building).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(building);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing building {BuildingId}", id);
                ViewBag.Error = "An error occurred while editing the building. Please try again later.";
                return View(building);
            }
        }

        // GET: /HostBuildings/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var building = await _context.Buildings.FindAsync(id);
                if (building == null) return NotFound();
                return View(building);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building {BuildingId}", id);
                ViewBag.Error = "An error occurred while deleting the building. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // POST: /HostBuildings/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var building = await _context.Buildings.FindAsync(id);
                if (building != null)
                {
                    _context.Buildings.Remove(building);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ViewBag.Error = "An error occurred while deleting the building. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /HostBuildings/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var building = await _context.Buildings
                    .Include(b => b.Rooms)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (building == null) return NotFound();
                return View(building);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading building details for building {BuildingId}", id);
                ViewBag.Error = "An error occurred loading the building details. Please try again later.";
                return RedirectToAction("Index");
            }
        }
    }
}
