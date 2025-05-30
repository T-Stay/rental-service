using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "customer")]
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /Reviews/Create
        [HttpPost]
        public async Task<IActionResult> Create(Guid roomId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = new Review
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                UserId = Guid.Parse(userId),
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }
    }
}
