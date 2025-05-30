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
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _context;
        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Favorites
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorites = await _context.Favorites
                .Include(f => f.Room)
                .ThenInclude(r => r.Building)
                .Where(f => f.UserId.ToString() == userId)
                .ToListAsync();
            return View(favorites);
        }

        // POST: /Favorites/Add/{roomId}
        [HttpPost]
        public async Task<IActionResult> Add(Guid roomId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!_context.Favorites.Any(f => f.UserId.ToString() == userId && f.RoomId == roomId))
            {
                var fav = new Favorite { Id = Guid.NewGuid(), UserId = Guid.Parse(userId), RoomId = roomId, CreatedAt = DateTime.UtcNow };
                _context.Favorites.Add(fav);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }

        // POST: /Favorites/Remove/{roomId}
        [HttpPost]
        public async Task<IActionResult> Remove(Guid roomId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId.ToString() == userId && f.RoomId == roomId);
            if (fav != null)
            {
                _context.Favorites.Remove(fav);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }
    }
}
