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
            if (!ad.IsActive && !isOwner)
            {
                // Chỉ chủ bài viết mới xem được bài chưa duyệt
                return NotFound();
            }
            return View(ad);
        }
    }
}
