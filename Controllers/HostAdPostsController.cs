using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostAdPostsController : Controller
    {
        private readonly AppDbContext _context;
        public HostAdPostsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /HostAdPosts/Create
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var packages = await _context.UserAdPackages
                .Where(p => p.UserId == userId && p.IsActive && p.RemainingPosts > 0 && p.ExpiryDate > DateTime.Now)
                .ToListAsync();
            // Lấy danh sách phòng của chủ trọ, kiểm tra null cho Building
            var rooms = await _context.Rooms
                .Include(r => r.Building)
                .Where(r => r.Building != null && r.Building.HostId.ToString() == userId)
                .ToListAsync();
            ViewBag.Packages = packages;
            ViewBag.Rooms = rooms;
            return View();
        }

        // POST: /HostAdPosts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdPost model, Guid selectedPackageId, List<Guid> selectedRoomIds)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var package = await _context.UserAdPackages.FirstOrDefaultAsync(p => p.Id == selectedPackageId && p.UserId == userId && p.IsActive && p.RemainingPosts > 0 && p.ExpiryDate > DateTime.Now);
            if (package == null)
            {
                ModelState.AddModelError("selectedPackageId", "Gói dịch vụ không hợp lệ hoặc đã hết lượt đăng.");
            }
            if (!selectedRoomIds.Any())
            {
                ModelState.AddModelError("selectedRoomIds", "Vui lòng chọn ít nhất một phòng để quảng cáo.");
            }
            if (ModelState.IsValid)
            {
                model.Id = Guid.NewGuid();
                model.HostId = userId ?? string.Empty;
                model.UserAdPackageId = package != null ? package.Id : Guid.Empty;
                model.PackageType = package != null ? package.PackageType : Models.AdPackageType.Free;
                model.CreatedAt = DateTime.Now;
                model.IsActive = false; // Chờ duyệt
                model.ViewCount = 0;
                model.Rooms = await _context.Rooms.Where(r => selectedRoomIds.Contains(r.Id)).ToListAsync();
                _context.AdPosts.Add(model);
                if (package != null) package.RemainingPosts--;
                await _context.SaveChangesAsync();
                return RedirectToAction("MyAdPosts");
            }
            // Reload lại packages và rooms nếu có lỗi
            ViewBag.Packages = await _context.UserAdPackages.Where(p => p.UserId == userId && p.IsActive && p.RemainingPosts > 0 && p.ExpiryDate > DateTime.Now).ToListAsync();
            ViewBag.Rooms = await _context.Rooms.Include(r => r.Building).Where(r => r.Building != null && r.Building.HostId.ToString() == userId).ToListAsync();
            return View(model);
        }

        // GET: /HostAdPosts/MyAdPosts
        public async Task<IActionResult> MyAdPosts()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var adPosts = await _context.AdPosts.Include(a => a.Rooms).Where(a => a.HostId == userId).ToListAsync();
            return View(adPosts);
        }
    }
}
