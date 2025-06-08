using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using RentalService.Services;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace RentalService.Controllers
{
    [Authorize(Roles = "host")]
    public class HostAdPostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly S3Service _s3Service;
        public HostAdPostsController(AppDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
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
        public async Task<IActionResult> Create(
            string title,
            string content,
            Guid selectedPackageId,
            List<Guid> selectedRoomIds,
            List<IFormFile> images)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var package = await _context.UserAdPackages.FirstOrDefaultAsync(p => p.Id == selectedPackageId && p.UserId == userId && p.IsActive && p.RemainingPosts > 0 && p.ExpiryDate > DateTime.Now);
            if (package == null)
            {
                ModelState.AddModelError("selectedPackageId", "Gói dịch vụ không hợp lệ hoặc đã hết lượt đăng.");
            }
            if (selectedRoomIds == null || !selectedRoomIds.Any())
            {
                ModelState.AddModelError("selectedRoomIds", "Vui lòng chọn ít nhất một phòng để quảng cáo.");
            }
            if (ModelState.IsValid)
            {
                var ad = new AdPost
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Content = content,
                    HostId = userId ?? string.Empty,
                    UserAdPackageId = package?.Id ?? Guid.Empty,
                    PackageType = package?.PackageType ?? AdPackageType.Free,
                    CreatedAt = DateTime.Now,
                    IsActive = false,
                    ViewCount = 0,
                    PriorityOrder = 0
                };
                // Badge theo gói
                var pkgType = package?.PackageType ?? AdPackageType.Free;
                switch (pkgType)
                {
                    case AdPackageType.KimCuong:
                        ad.Badge = "Vip Kim Cương";
                        break;
                    case AdPackageType.Vang:
                        ad.Badge = "Vip Vàng";
                        break;
                    case AdPackageType.Bac:
                        ad.Badge = "Vip Bạc";
                        break;
                    default:
                        ad.Badge = string.Empty;
                        break;
                }
                // Xử lý upload ảnh lên S3
                var imageUrls = new List<string>();
                if (images != null)
                {
                    foreach (var file in images)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"adposts/{ad.Id}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            using (var stream = file.OpenReadStream())
                            {
                                var url = await _s3Service.UploadFileAsync(stream, fileName, file.ContentType);
                                imageUrls.Add(url);
                            }
                        }
                    }
                }
                ad.ImageUrls = string.Join(",", imageUrls);
                ad.Rooms = selectedRoomIds != null ? await _context.Rooms.Where(r => selectedRoomIds.Contains(r.Id)).ToListAsync() : new List<Room>();
                _context.AdPosts.Add(ad);
                if (package != null) package.RemainingPosts--;
                await _context.SaveChangesAsync();
                return RedirectToAction("MyAdPosts");
            }
            // Reload lại packages và rooms nếu có lỗi
            ViewBag.Packages = await _context.UserAdPackages.Where(p => p.UserId == userId && p.IsActive && p.RemainingPosts > 0 && p.ExpiryDate > DateTime.Now).ToListAsync();
            ViewBag.Rooms = await _context.Rooms.Include(r => r.Building).Where(r => r.Building != null && r.Building.HostId.ToString() == userId).ToListAsync();
            return View();
        }

        // GET: /HostAdPosts/MyAdPosts
        public async Task<IActionResult> MyAdPosts(string packageType, string status, string q, string sort)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var adsQuery = _context.AdPosts
                .Include(a => a.Rooms)
                .Where(a => a.HostId == userId);

            // Filter by packageType
            if (!string.IsNullOrEmpty(packageType))
            {
                if (Enum.TryParse<RentalService.Models.AdPackageType>(packageType, out var pkg))
                {
                    adsQuery = adsQuery.Where(a => a.PackageType == pkg);
                }
            }
            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "approved")
                    adsQuery = adsQuery.Where(a => a.IsActive);
                else if (status == "pending")
                    adsQuery = adsQuery.Where(a => !a.IsActive);
            }
            // Search by title
            if (!string.IsNullOrEmpty(q))
            {
                adsQuery = adsQuery.Where(a => a.Title.Contains(q));
            }
            // Sort
            switch (sort)
            {
                case "created_asc":
                    adsQuery = adsQuery.OrderBy(a => a.CreatedAt);
                    break;
                case "views_desc":
                    adsQuery = adsQuery.OrderByDescending(a => a.ViewCount);
                    break;
                case "views_asc":
                    adsQuery = adsQuery.OrderBy(a => a.ViewCount);
                    break;
                default:
                    adsQuery = adsQuery.OrderByDescending(a => a.CreatedAt);
                    break;
            }
            var ads = await adsQuery.ToListAsync();
            return View(ads);
        }
    }
}
