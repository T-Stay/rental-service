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

        // GET: /Admin/AdPostsToApprove
        public IActionResult AdPostsToApprove()
        {
            // Lấy các bài quảng cáo chưa duyệt
            var ads = _context.AdPosts
                .Include(a => a.Rooms)
                .Include(a => a.UserAdPackage)
                .Where(a => !a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            return View(ads);
        }

        // POST: /Admin/ApproveAdPost/{id}
        [HttpPost]
        public IActionResult ApproveAdPost(Guid id)
        {
            var ad = _context.AdPosts.FirstOrDefault(a => a.Id == id);
            if (ad != null)
            {
                ad.IsActive = true;
                _context.SaveChanges();
                // TODO: Gửi thông báo cho chủ trọ
            }
            return RedirectToAction("AdPostsToApprove");
        }

        // POST: /Admin/HideAdPost/{id}
        [HttpPost]
        public IActionResult HideAdPost(Guid id)
        {
            var ad = _context.AdPosts.FirstOrDefault(a => a.Id == id);
            if (ad != null)
            {
                ad.IsActive = false;
                _context.SaveChanges();
            }
            return RedirectToAction("AdPostsToApprove");
        }

        // POST: /Admin/DeleteAdPost/{id}
        [HttpPost]
        public IActionResult DeleteAdPost(Guid id)
        {
            var ad = _context.AdPosts.FirstOrDefault(a => a.Id == id);
            if (ad != null)
            {
                _context.AdPosts.Remove(ad);
                _context.SaveChanges();
            }
            return RedirectToAction("AdPostsToApprove");
        }

        // GET: /Admin/UnapprovedRooms
        public IActionResult UnapprovedRooms()
        {
            // Lấy danh sách phòng chưa duyệt (Inactive)
            var rooms = _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomImages)
                .Where(r => r.Status == RoomStatus.Inactive)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            return View(rooms);
        }

        // Add more actions for role management as needed
    }
}
