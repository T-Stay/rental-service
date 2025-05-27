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
    public class HostBuildingsController : Controller
    {
        private readonly AppDbContext _context;
        public HostBuildingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /HostBuildings
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var buildings = await _context.Buildings
                .Where(b => b.HostId.ToString() == userId)
                .Include(b => b.Rooms)
                .ToListAsync();
            return View(buildings);
        }

        // GET: /HostBuildings/Create
        public IActionResult Create() => View();

        // POST: /HostBuildings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Building building)
        {
            // log the incoming building data
            Console.WriteLine($"Creating building: {building.Name}, Address: {building.Address}, Description: {building.Description}, Location: {building.Location}");
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

        // GET: /HostBuildings/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: /HostBuildings/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Building building)
        {
            if (id != building.Id) return NotFound();
            if (ModelState.IsValid)
            {
                building.UpdatedAt = DateTime.UtcNow;
                _context.Update(building);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(building);
        }

        // GET: /HostBuildings/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: /HostBuildings/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building != null)
            {
                _context.Buildings.Remove(building);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
