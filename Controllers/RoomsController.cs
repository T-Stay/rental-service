using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using RentalService.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

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
        public async Task<IActionResult> Index(string location, decimal? minPrice, decimal? maxPrice, Guid? buildingId, string[] amenities, string sort, int? page)
        {
            int pageSize = 9;
            int pageNumber = page ?? 1;
            var rooms = _context.Rooms
                .Include(r => r.Images)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)
                .Include(r => r.Reviews)
                    .ThenInclude(review => review.User)
                .Include(r => r.Building)
                .AsQueryable()
                .Where(r => r.Status == RoomStatus.Active);
            if (buildingId.HasValue)
                rooms = rooms.Where(r => r.BuildingId == buildingId);
            if (!string.IsNullOrEmpty(location))
                rooms = rooms.Where(r => r.Building != null && r.Building.Location.Contains(location));
            if (minPrice.HasValue)
                rooms = rooms.Where(r => r.Price >= minPrice);
            if (maxPrice.HasValue)
                rooms = rooms.Where(r => r.Price <= maxPrice);
            // Chỉ áp dụng sắp xếp khi bấm nút Lọc
            // ... filter amenities ...
            if (amenities != null && amenities.Length > 0)
            {
                var amenityGuids = amenities.Select(a => Guid.Parse(a)).ToList();
                var roomListEF = await rooms.ToListAsync();
                roomListEF = roomListEF.Where(r => r.Amenities != null && r.Amenities.Any(a => amenityGuids.Contains(a.Id))).ToList();
                roomListEF = sort switch
                {
                    "price_asc" => roomListEF.OrderBy(r => r.Price).ToList(),
                    "price_desc" => roomListEF.OrderByDescending(r => r.Price).ToList(),
                    "rating_desc" => roomListEF.OrderByDescending(r => (r.Reviews != null && r.Reviews.Any()) ? r.Reviews.Average(rv => rv.Rating) : 0).ToList(),
                    _ => roomListEF.OrderByDescending(r => r.CreatedAt).ToList()
                };
                ViewBag.Buildings = await _context.Buildings.ToListAsync();
                ViewBag.SelectedBuildingId = buildingId;
                ViewBag.Amenities = await _context.Amenities.ToListAsync();
                return View(roomListEF.ToPagedList(pageNumber, pageSize));
            }
            if (sort == "rating_desc")
            {
                var roomList = await rooms.ToListAsync();
                roomList = roomList.OrderByDescending(r => (r.Reviews != null && r.Reviews.Any()) ? r.Reviews.Average(rv => rv.Rating) : 0).ToList();
                ViewBag.Buildings = await _context.Buildings.ToListAsync();
                ViewBag.SelectedBuildingId = buildingId;
                ViewBag.Amenities = await _context.Amenities.ToListAsync();
                return View(roomList.ToPagedList(pageNumber, pageSize));
            }
            switch (sort)
            {
                case "price_asc":
                    rooms = rooms.OrderBy(r => r.Price);
                    break;
                case "price_desc":
                    rooms = rooms.OrderByDescending(r => r.Price);
                    break;
                default:
                    rooms = rooms.OrderByDescending(r => r.CreatedAt);
                    break;
            }
            ViewBag.Buildings = await _context.Buildings.ToListAsync();
            ViewBag.SelectedBuildingId = buildingId;
            ViewBag.Amenities = await _context.Amenities.ToListAsync();
            var roomList2 = await rooms.ToListAsync();
            return View(roomList2.ToPagedList(pageNumber, pageSize));
        }

        // GET: /Rooms/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var room = await _context.Rooms
                .Include(r => r.Images)
                .Include(r => r.RoomImages)
                .Include(r => r.Amenities)
                .Include(r => r.Building)
                .ThenInclude(b => b.Host)
                .ThenInclude(h => h.ContactInformations)
                .Include(r => r.Reviews)
                .ThenInclude(review => review.User)
                .Include(r => r.BookingRequests)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            // Check if current user has favorited this room
            bool isFavorite = false;
            bool canReview = false;
            if (User?.Identity != null && User.Identity.IsAuthenticated && (User.FindFirst("role")?.Value == "customer"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorite = await _context.Favorites.AnyAsync(f => f.UserId.ToString() == userId && f.RoomId == id);
                    canReview = await _context.BookingRequests.AnyAsync(b => b.UserId.ToString() == userId && b.RoomId == id && b.Status == BookingRequestStatus.Approved);
                }
            }
            ViewBag.IsFavorite = isFavorite;
            ViewBag.CanReview = canReview;
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
