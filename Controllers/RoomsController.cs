using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using RentalService.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly S3Service _s3Service;
        public RoomsController(AppDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
        }

        // GET: /Rooms
        public async Task<IActionResult> Index(string location, decimal? minPrice, decimal? maxPrice, Guid? buildingId)
        {
            var rooms = _context.Rooms
                .Include(r => r.Images)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)
                .Include(r => r.Reviews)
                    .ThenInclude(review => review.User)
                .Include(r => r.Building).AsQueryable();
            if (buildingId.HasValue)
                rooms = rooms.Where(r => r.BuildingId == buildingId);
            if (!string.IsNullOrEmpty(location))
                rooms = rooms.Where(r => r.Building != null && r.Building.Location.Contains(location));
            if (minPrice.HasValue)
                rooms = rooms.Where(r => r.Price >= minPrice);
            if (maxPrice.HasValue)
                rooms = rooms.Where(r => r.Price <= maxPrice);
            ViewBag.Buildings = await _context.Buildings.ToListAsync();
            ViewBag.SelectedBuildingId = buildingId;
            return View(await rooms.ToListAsync());
        }

        // GET: /Rooms/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var room = await _context.Rooms
                .Include(r => r.Images)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)
                .Include(r => r.Building)
                .Include(r => r.Reviews)
                    .ThenInclude(review => review.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            // Check if current user has favorited this room
            bool isFavorite = false;
            if (User?.Identity != null && User.Identity.IsAuthenticated && (User.FindFirst("role")?.Value == "customer"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorite = await _context.Favorites.AnyAsync(f => f.UserId.ToString() == userId && f.RoomId == id);
                }
            }
            ViewBag.IsFavorite = isFavorite;
            return View(room);
        }

        // GET: /Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                room.Id = Guid.NewGuid();
                room.CreatedAt = DateTime.UtcNow;
                room.UpdatedAt = DateTime.UtcNow;
                if (imageFile != null && imageFile.Length > 0)
                {
                    using var stream = imageFile.OpenReadStream();
                    var imageUrl = await _s3Service.UploadFileAsync(stream, $"rooms/{room.Id}/{imageFile.FileName}", imageFile.ContentType);
                    room.Images = new List<RoomImage> { new RoomImage { Id = Guid.NewGuid(), RoomId = room.Id, ImageUrl = imageUrl } };
                }
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }
    }
}
