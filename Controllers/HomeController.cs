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

    public async Task<IActionResult> Index(string? search)
    {
        // if authenticated, redirect to the appropriate dashboard
        try
        {
            if (User.Identity.IsAuthenticated)
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
            // Handle the error as needed, e.g., show an error page or log it
        }
        var query = _context.AdPosts
            .Include(a => a.UserAdPackage)
            .Include(a => a.Rooms)
            .Where(a => a.IsActive && a.UserAdPackage.IsActive && a.UserAdPackage.ExpiryDate > DateTime.Now);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Title.Contains(search) || a.Content.Contains(search));
            ViewBag.Search = search;
        }
        var ads = await query
            .OrderByDescending(a => a.PackageType)
            .ThenBy(a => a.PriorityOrder)
            .Take(12)
            .ToListAsync();
        return View(ads);
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
