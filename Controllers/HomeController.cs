using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System.Security.Claims;

namespace _.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, decimal? minPrice, decimal? maxPrice, string[] amenities, string sort, double? centerLat, double? centerLng, double? radius, string advanceAddress, double? minArea, double? maxArea)
    {
        try
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication check in HomeController.Index");
        }
        var query = _context.AdPosts
            .Include(a => a.UserAdPackage)
            .Include(a => a.Rooms)
                .ThenInclude(r => r.Amenities)
            .Include(a => a.Rooms)
                .ThenInclude(r => r.Building)
            .Where(a => a.IsActive && a.UserAdPackage.IsActive && a.UserAdPackage.ExpiryDate > DateTime.Now);
        ViewBag.Amenities = await _context.Amenities.ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Title.Contains(search) || a.Content.Contains(search));
            ViewBag.Search = search;
        }
        var adList = await query
            .OrderByDescending(a => a.PackageType)
            .ThenBy(a => a.PriorityOrder)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
        // Lọc theo phòng bên trong quảng cáo
        if (minPrice.HasValue)
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r => r.Price >= minPrice)).ToList();
        if (maxPrice.HasValue)
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r => r.Price <= maxPrice)).ToList();
        if (minArea.HasValue)
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r => r.Area >= minArea)).ToList();
        if (maxArea.HasValue)
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r => r.Area <= maxArea)).ToList();
        if (amenities != null && amenities.Length > 0)
        {
            var amenityGuids = amenities.Select(a => Guid.Parse(a)).ToList();
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r => r.Amenities != null && amenityGuids.All(ag => r.Amenities.Any(a => a.Id == ag)))).ToList();
        }
        // Lọc theo vị trí nâng cao (bán kính, lat/lng)
        if (centerLat.HasValue && centerLng.HasValue && radius.HasValue && radius > 0)
        {
            double toRad(double deg) => deg * Math.PI / 180.0;
            double haversine(double lat1, double lon1, double lat2, double lon2)
            {
                double R = 6371;
                double dLat = toRad(lat2 - lat1);
                double dLon = toRad(lon2 - lon1);
                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(toRad(lat1)) * Math.Cos(toRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return R * c;
            }
            adList = adList.Where(ad => ad.Rooms != null && ad.Rooms.Any(r =>
                r.Building != null &&
                !string.IsNullOrEmpty(r.Building.Location) &&
                r.Building.Location.Contains(",") &&
                double.TryParse(r.Building.Location.Split(',')[0], out double lat) &&
                double.TryParse(r.Building.Location.Split(',')[1], out double lng) &&
                haversine(centerLat.Value, centerLng.Value, lat, lng) <= radius.Value
            )).ToList();
        }
        // Sắp xếp
        switch (sort)
        {
            case "price_asc":
                adList = adList.OrderBy(ad => ad.Rooms != null && ad.Rooms.Any() ? ad.Rooms.Min(r => r.Price) : decimal.MaxValue).ToList();
                break;
            case "price_desc":
                adList = adList.OrderByDescending(ad => ad.Rooms != null && ad.Rooms.Any() ? ad.Rooms.Max(r => r.Price) : decimal.MinValue).ToList();
                break;
            case "area_asc":
                adList = adList.OrderBy(ad => ad.Rooms != null && ad.Rooms.Any() ? ad.Rooms.Min(r => r.Area) : double.MaxValue).ToList();
                break;
            case "area_desc":
                adList = adList.OrderByDescending(ad => ad.Rooms != null && ad.Rooms.Any() ? ad.Rooms.Max(r => r.Area) : double.MinValue).ToList();
                break;
            default:
                adList = adList.OrderByDescending(ad => ad.PackageType).ThenBy(ad => ad.PriorityOrder).ToList();
                break;
        }
        // Giới hạn số lượng hiển thị (giữ nguyên Take(12) như cũ)
        adList = adList.Take(12).ToList();
        return View(adList);
    }

    // Demo layout: render Index2.cshtml
    public async Task<IActionResult> Index2()
    {
        var ads = await _context.AdPosts
            .Include(a => a.UserAdPackage)
            .Include(a => a.Rooms)
            .Where(a => a.IsActive && a.UserAdPackage.IsActive && a.UserAdPackage.ExpiryDate > DateTime.Now)
            .OrderByDescending(a => a.PackageType)
            .ThenBy(a => a.PriorityOrder)
            .Take(20)
            .ToListAsync();
        return View("Index2", ads);
    }

    [Authorize(Roles = "customer")]
    public async Task<IActionResult> CustomerDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Check contact info count
        var contactInfoCount = await _context.ContactInformations.CountAsync(c => c.UserId.ToString() == userId);
        ViewBag.ContactInfoIncomplete = contactInfoCount < 2;
        var favorites = await _context.Favorites
            .Include(f => f.Room)
            .ThenInclude(r => r.Building!) // null-forgiving operator
            .Where(f => f.UserId.ToString() == userId)
            .ToListAsync();
        var bookings = await _context.BookingRequests
            .Include(b => b.Room)
            .Where(b => b.UserId.ToString() == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        var appointments = await _context.ViewAppointments
            .Include(a => a.Room)
            .Where(a => a.UserId.ToString() == userId)
            .OrderByDescending(a => a.AppointmentTime)
            .ToListAsync();
        // Get total active rooms
        int totalActiveRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Active);
        var notifications = await _context.Notifications
            .Where(n => n.UserId.ToString() == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        ViewBag.Favorites = favorites;
        ViewBag.BookingRequests = bookings;
        ViewBag.Appointments = appointments;
        ViewBag.TotalActiveRooms = totalActiveRooms;
        ViewBag.Notifications = notifications;
        return View("~/Views/CustomerDashboard/Index.cshtml");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize(Roles = "customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifications = await _context.Notifications
            .Where(n => n.UserId.ToString() == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in notifications)
        {
            n.IsRead = true;
        }
        await _context.SaveChangesAsync();
        return RedirectToAction("CustomerDashboard");
    }
}
