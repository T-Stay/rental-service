using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalService.Data;
using RentalService.Models;

namespace RentalService.Controllers
{
    [AllowAnonymous]
    public class AdPostsController : Controller
    {
        private readonly AppDbContext _context;
        public AdPostsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdPosts/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var ad = await _context.AdPosts
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.RoomImages)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Building)
                .Include(a => a.UserAdPackage)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (ad == null)
                return NotFound();

            string? userId = null;
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
            var isOwner = userId != null && ad.HostId == userId;
            var isAdmin = User?.IsInRole("admin") == true;
            if (!ad.IsActive && !isOwner && !isAdmin)
            {
                // Chỉ chủ bài viết hoặc admin mới xem được bài chưa duyệt
                return NotFound();
            }
            return View(ad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncreaseViewCount(Guid id)
        {
            var userId = User?.Identity != null && User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            if (!string.IsNullOrEmpty(userId))
            {
                // Sử dụng key session riêng cho từng user, lưu id bài cuối cùng đã xem
                var sessionKey = $"lastPost_{userId}";
                var lastPost = HttpContext.Session.GetString(sessionKey);
                if (lastPost == id.ToString())
                {
                    return Ok(); // Không tăng view nếu vừa xem bài này
                }
                HttpContext.Session.SetString(sessionKey, id.ToString());
            }
            var ad = _context.AdPosts.FirstOrDefault(a => a.Id == id);
            if (ad != null)
            {
                ad.ViewCount = ad.ViewCount + 1;
                _context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }
    }
}
