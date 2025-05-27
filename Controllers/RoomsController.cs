using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Rooms
        public async Task<IActionResult> Index(string location, decimal? minPrice, decimal? maxPrice)
        {
            var rooms = _context.Rooms.Include(r => r.Images).Include(r => r.Amenities).Include(r => r.Building).AsQueryable();
            if (!string.IsNullOrEmpty(location))
                rooms = rooms.Where(r => r.Building.Location.Contains(location));
            if (minPrice.HasValue)
                rooms = rooms.Where(r => r.Price >= minPrice);
            if (maxPrice.HasValue)
                rooms = rooms.Where(r => r.Price <= maxPrice);
            return View(await rooms.ToListAsync());
        }

        // GET: /Rooms/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var room = await _context.Rooms
                .Include(r => r.Images)
                .Include(r => r.Amenities)
                .Include(r => r.Building)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }
    }
}
