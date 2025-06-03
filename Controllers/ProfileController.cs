using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProfileController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.Include(u => u.ContactInformations).FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddContact(ContactType type, string data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound();
            // Basic validation
            if (type == ContactType.Email && !data.Contains("@")) ModelState.AddModelError("data", "Invalid email");
            if (type == ContactType.PhoneNumber && data.Length < 8) ModelState.AddModelError("data", "Invalid phone number");
            if (!ModelState.IsValid) return RedirectToAction("Index");
            var contact = new ContactInformation { Id = Guid.NewGuid(), UserId = user.Id, Type = type, Data = data, CreatedAt = DateTime.UtcNow };
            _context.ContactInformations.Add(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContact(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contact = await _context.ContactInformations.FirstOrDefaultAsync(c => c.Id == id && c.UserId.ToString() == userId);
            if (contact != null)
            {
                _context.ContactInformations.Remove(contact);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId.ToString() == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(12)
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("g")
                })
                .ToListAsync();
            return Json(notifications);
        }
    }
}
