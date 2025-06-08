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
                .Include(a => a.UserAdPackage)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
            if (ad == null) return NotFound();
            return View(ad);
        }
    }
}
