using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RentalService.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly AppDbContext _context;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // GET: /Admin/RoomsToApprove
        public IActionResult RoomsToApprove()
        {
            // Get rooms with Inactive status (skip .Include if navigation property missing)
            var rooms = _context.Rooms
                .Where(r => r.Status == RoomStatus.Inactive)
                .ToList();
            return View(rooms);
        }

        // POST: /Admin/ApproveRoom/{id}
        [HttpPost]
        public IActionResult ApproveRoom(Guid id)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == id);
            if (room != null)
            {
                room.Status = RoomStatus.Active;
                _context.SaveChanges();
            }
            return RedirectToAction("RoomsToApprove");
        }

        // Add more actions for role management as needed
    }
}
