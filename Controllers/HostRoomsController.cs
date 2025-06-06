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
using Microsoft.AspNetCore.Http;
using RentalService.Services;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostRoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HostRoomsController> _logger;
        private readonly S3Service _s3Service;
        public HostRoomsController(AppDbContext context, ILogger<HostRoomsController> logger, S3Service s3Service)
        {
            _context = context;
            _logger = logger;
            _s3Service = s3Service;
        }

        // GET: /HostRooms?buildingId={buildingId}
        public async Task<IActionResult> Index(Guid? buildingId, double? minArea, double? maxArea, string sort, string search, string status)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var query = _context.Rooms.Include(r => r.Building)
                    .Where(r => r.Building != null && r.Building.HostId.ToString() == userId);
                if (buildingId.HasValue)
                    query = query.Where(r => r.BuildingId == buildingId);
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(r => r.Name.Contains(search));
                if (minArea.HasValue)
                    query = query.Where(r => r.Area >= minArea.Value);
                if (maxArea.HasValue)
                    query = query.Where(r => r.Area <= maxArea.Value);
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<RoomStatus>(status, out var statusEnum))
                    {
                        query = query.Where(r => r.Status == statusEnum);
                    }
                }
                switch (sort)
                {
                    case "price_asc":
                        query = query.OrderBy(r => r.Price);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(r => r.Price);
                        break;
                    case "area_asc":
                        query = query.OrderBy(r => r.Area);
                        break;
                    case "area_desc":
                        query = query.OrderByDescending(r => r.Area);
                        break;
                    case "name_asc":
                        query = query.OrderBy(r => r.Name);
                        break;
                    case "name_desc":
                        query = query.OrderByDescending(r => r.Name);
                        break;
                    default:
                        query = query.OrderByDescending(r => r.CreatedAt);
                        break;
                }
                var rooms = await query
                    .Include(r => r.RoomImages)
                    .ToListAsync();
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
        public async Task<IActionResult> Create(Room room, List<IFormFile> RoomImages)
        {
            try
            {
                // Xử lý thêm phòng nhanh
                var quickAdd = Request.Form["QuickAdd"].FirstOrDefault() == "on";
                if (quickAdd)
                {
                    int count = 0;
                    int.TryParse(Request.Form["QuickAddCount"], out count);
                    var nameFormat = Request.Form["QuickAddNameFormat"].FirstOrDefault() ?? "Phòng A{0}";
                    int start = 1;
                    int.TryParse(Request.Form["QuickAddStart"], out start);
                    double area = 0;
                    double.TryParse(Request.Form["Area"], out area);
                    if (count > 1)
                    {
                        var quickAmenityIds = Request.Form["AmenityIds"].ToList();
                        var amenities = quickAmenityIds.Any() ? await _context.Amenities.Where(a => quickAmenityIds.Contains(a.Id.ToString())).ToListAsync() : new List<Amenity>();
                        var uploadedImages = new List<RoomImage>();
                        if (RoomImages != null && RoomImages.Count > 0)
                        {
                            foreach (var image in RoomImages)
                            {
                                if (image.Length > 0)
                                {
                                    using var stream = image.OpenReadStream();
                                    var fileName = $"rooms/{Guid.NewGuid()}_{image.FileName}";
                                    var imageUrl = await _s3Service.UploadFileAsync(stream, fileName, image.ContentType);
                                    uploadedImages.Add(new RoomImage { Id = Guid.NewGuid(), ImageUrl = imageUrl });
                                }
                            }
                        }
                        for (int i = 0; i < count; i++)
                        {
                            var newRoom = new Room
                            {
                                Id = Guid.NewGuid(),
                                BuildingId = room.BuildingId,
                                Name = string.Format(nameFormat, start + i),
                                Description = room.Description,
                                Price = room.Price,
                                Status = room.Status,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                Amenities = new List<Amenity>(amenities),
                                Area = area
                            };
                            _context.Rooms.Add(newRoom);
                            // Tạo RoomImages riêng cho từng phòng, nhưng dùng chung URL
                            if (uploadedImages.Count > 0)
                            {
                                foreach (var img in uploadedImages)
                                {
                                    var roomImg = new RoomImage
                                    {
                                        Id = Guid.NewGuid(),
                                        RoomId = newRoom.Id,
                                        ImageUrl = img.ImageUrl
                                    };
                                    _context.RoomImages.Add(roomImg);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index), new { buildingId = room.BuildingId });
                    }
                }
                // Handle amenities
                var amenityIds = Request.Form["AmenityIds"].ToList();
                if (amenityIds.Any())
                {
                    room.Amenities = await _context.Amenities.Where(a => amenityIds.Contains(a.Id.ToString())).ToListAsync();
                }
                if (ModelState.IsValid)
                {
                    room.Id = Guid.NewGuid();
                    room.CreatedAt = room.UpdatedAt = DateTime.UtcNow;
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync(); // Save room first to get valid RoomId

                    // Handle image upload and save RoomImages
                    var uploadedImages = new List<RoomImage>();
                    if (RoomImages != null && RoomImages.Count > 0)
                    {
                        foreach (var image in RoomImages)
                        {
                            if (image.Length > 0)
                            {
                                using var stream = image.OpenReadStream();
                                var fileName = $"rooms/{Guid.NewGuid()}_{image.FileName}";
                                var imageUrl = await _s3Service.UploadFileAsync(stream, fileName, image.ContentType);
                                uploadedImages.Add(new RoomImage { Id = Guid.NewGuid(), RoomId = room.Id, ImageUrl = imageUrl });
                            }
                        }
                        if (uploadedImages.Count > 0)
                        {
                            _context.RoomImages.AddRange(uploadedImages);
                            await _context.SaveChangesAsync();
                        }
                    }
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
                // make the price integer
                room.Price = (int)room.Price; // Assuming Price is a decimal, convert to int for displa
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
        public async Task<IActionResult> Edit(Guid id, Room room, List<IFormFile> RoomImages, List<Guid> RemoveImageIds)
        {
            if (id != room.Id) return NotFound();
            try
            {
                // Handle amenities
                var amenityIds = Request.Form["AmenityIds"].ToList();
                var selectedAmenities = new List<Amenity>();
                if (amenityIds.Any())
                {
                    var amenityGuids = amenityIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => Guid.Parse(x!)).ToList();
                    selectedAmenities = await _context.Amenities.Where(a => amenityGuids.Contains(a.Id)).ToListAsync();
                }
                var dbRoom = await _context.Rooms.Include(r => r.RoomImages).Include(r => r.Amenities).FirstOrDefaultAsync(r => r.Id == id);
                if (dbRoom == null) return NotFound();
                // Only update the tracked dbRoom instance
                dbRoom.Name = room.Name;
                dbRoom.Description = room.Description;
                dbRoom.Price = room.Price;
                dbRoom.Status = room.Status;
                dbRoom.BuildingId = room.BuildingId;
                dbRoom.UpdatedAt = DateTime.UtcNow;
                dbRoom.Area = room.Area;
                // Only add new amenities and remove missing ones to avoid duplicates
                if (dbRoom.Amenities == null)
                {
                    dbRoom.Amenities = new List<Amenity>();
                }
                var currentAmenityIds = dbRoom.Amenities.Select(a => a.Id).ToHashSet();
                var newAmenityIds = selectedAmenities.Select(a => a.Id).ToHashSet();
                // Remove amenities that are no longer selected
                foreach (var a in dbRoom.Amenities.Where(a => !newAmenityIds.Contains(a.Id)).ToList())
                {
                    dbRoom.Amenities.Remove(a);
                }
                // Add new amenities that are not already present
                foreach (var a in selectedAmenities)
                {
                    if (!currentAmenityIds.Contains(a.Id))
                    {
                        dbRoom.Amenities.Add(a);
                    }
                }
                // Remove selected images (from DB and S3)
                if (RemoveImageIds != null && RemoveImageIds.Count > 0)
                {
                    if (dbRoom.RoomImages == null)
                        dbRoom.RoomImages = new List<RoomImage>();
                    var imagesToRemove = dbRoom.RoomImages.Where(img => RemoveImageIds.Contains(img.Id)).ToList();
                    foreach (var img in imagesToRemove)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
                        {
                            // Remove from S3
                            var key = img.ImageUrl.Split(new[] { ".amazonaws.com/" }, StringSplitOptions.None).LastOrDefault();
                            if (!string.IsNullOrEmpty(key))
                            {
                                await _s3Service.DeleteFileAsync(key);
                            }
                        }
                        _context.RoomImages.Remove(img);
                    }
                }
                // Upload new images
                if (RoomImages != null && RoomImages.Count > 0)
                {
                    if (dbRoom.RoomImages == null)
                        dbRoom.RoomImages = new List<RoomImage>();
                    foreach (var image in RoomImages)
                    {
                        if (image.Length > 0)
                        {
                            using var stream = image.OpenReadStream();
                            var fileName = $"rooms/{Guid.NewGuid()}_{image.FileName}";
                            var imageUrl = await _s3Service.UploadFileAsync(stream, fileName, image.ContentType);
                            var newRoomImage = new RoomImage { Id = Guid.NewGuid(), RoomId = dbRoom.Id, ImageUrl = imageUrl };
                            dbRoom.RoomImages.Add(newRoomImage); // Add to tracked collection
                            _context.RoomImages.Add(newRoomImage); // Add to DbSet
                        }
                    }
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index), new { buildingId = dbRoom.BuildingId });
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _logger.LogError("Concurrency error editing room {RoomId}", id);
                        ModelState.AddModelError(string.Empty, "The room was modified by another user. Please reload and try again.");
                        // Optionally, reload dbRoom from the database here
                    }
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Buildings = await _context.Buildings.Where(b => b.HostId.ToString() == userId).ToListAsync();
                ViewBag.SelectedBuildingId = dbRoom.BuildingId;
                ViewBag.AllAmenities = await _context.Amenities.ToListAsync();
                ViewBag.SelectedAmenities = amenityIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => Guid.Parse(x!)).ToList();
                return View(dbRoom);
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
                var room = await _context.Rooms
                    .Include(r => r.BookingRequests)
                    .Include(r => r.RoomImages)
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (room != null)
                {
                    if (room.BookingRequests != null && room.BookingRequests.Any())
                    {
                        TempData["ToastError"] = "Cannot delete room with existing booking requests.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Delete all related S3 images (RoomImages and Images)
                    var allImages = new List<RoomImage>();
                    if (room.RoomImages != null)
                        allImages.AddRange(room.RoomImages);
                    if (room.Images != null)
                        allImages.AddRange(room.Images);
                    foreach (var img in allImages)
                    {
                        if (!string.IsNullOrEmpty(img.ImageUrl))
                        {
                            var key = img.ImageUrl.Split(new[] { ".amazonaws.com/" }, StringSplitOptions.None).LastOrDefault();
                            if (!string.IsNullOrEmpty(key))
                            {
                                await _s3Service.DeleteFileAsync(key);
                            }
                        }
                    }

                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                    TempData["ToastSuccess"] = "Room deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room {RoomId}", id);
                TempData["ToastError"] = "An error occurred while deleting the room. Please try again later.";
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
                    .Include(r => r.RoomImages)
                    .Include(r => r.Amenities)
                    .Include(r => r.Building)
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
