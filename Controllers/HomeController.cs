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

    public IActionResult Index()
    {
        // if authenticated, redirect to the appropriate dashboard
        try
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("admin"))
                {
                    return RedirectToAction("AdminDashboard", "Admin");
                }
                else if (User.IsInRole("customer"))
                {
                    return RedirectToAction("CustomerDashboard", "Home");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication check in HomeController.Index");
            // Handle the error as needed, e.g., show an error page or log it
        }
        return View();
    }

    [Authorize(Roles = "customer")]
    public async Task<IActionResult> CustomerDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
        ViewBag.Favorites = favorites;
        ViewBag.BookingRequests = bookings;
        ViewBag.Appointments = appointments;
        ViewBag.TotalActiveRooms = totalActiveRooms;
        return View("~/Views/CustomerDashboard/Index.cshtml");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
