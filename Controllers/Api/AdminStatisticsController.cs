using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalService.Data;
using RentalService.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace RentalService.Controllers.Api
{
    [Route("api/admin/statistics")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminStatisticsController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        public AdminStatisticsController(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("overview")]
        public IActionResult Overview()
        {
            return Ok(new {
                totalUsers = _userManager.Users.Count(),
                totalRooms = _context.Rooms.Count(),
                totalBuildings = _context.Buildings.Count(),
                totalAdPosts = _context.AdPosts.Count(),
                pendingAds = _context.AdPosts.Count(a => !a.IsActive),
                pendingRooms = _context.Rooms.Count(r => r.Status == RoomStatus.Inactive)
            });
        }

        [HttpGet("growth")]
        public IActionResult Growth(string unit = "month", int range = 12)
        {
            DateTime now = DateTime.UtcNow;
            Func<DateTime, DateTime> stepBack;
            Func<DateTime, string> labelFormat;
            switch (unit)
            {
                case "day":
                    stepBack = d => d.AddDays(-1);
                    labelFormat = d => d.ToString("dd/MM");
                    break;
                case "week":
                    stepBack = d => d.AddDays(-7);
                    labelFormat = d => $"Tuần {System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday)}/{d.Year}";
                    break;
                case "year":
                    stepBack = d => d.AddYears(-1);
                    labelFormat = d => d.ToString("yyyy");
                    break;
                default:
                    stepBack = d => d.AddMonths(-1);
                    labelFormat = d => d.ToString("MM/yyyy");
                    break;
            }
            var timePoints = Enumerable.Range(0, range)
                .Select(i => {
                    var d = now;
                    for (int j = 0; j < i; j++) d = stepBack(d);
                    return d;
                })
                .Reverse()
                .ToList();

            // Đảm bảo lấy dữ liệu ra khỏi IQueryable trước khi lặp
            var users = _userManager.Users.ToList();
            var adPosts = _context.AdPosts.ToList();
            var rooms = _context.Rooms.ToList();

            var userGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => users.Count(u => u.CreatedAt.Date == d.Date),
                    "week" => users.Count(u => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(u.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && u.CreatedAt.Year == d.Year),
                    "year" => users.Count(u => u.CreatedAt.Year == d.Year),
                    _ => users.Count(u => u.CreatedAt.Year == d.Year && u.CreatedAt.Month == d.Month)
                }
            }).ToList();
            var adPostGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => adPosts.Count(a => a.CreatedAt.Date == d.Date),
                    "week" => adPosts.Count(a => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(a.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && a.CreatedAt.Year == d.Year),
                    "year" => adPosts.Count(a => a.CreatedAt.Year == d.Year),
                    _ => adPosts.Count(a => a.CreatedAt.Year == d.Year && a.CreatedAt.Month == d.Month)
                }
            }).ToList();
            var roomGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => rooms.Count(r => r.CreatedAt.Date == d.Date),
                    "week" => rooms.Count(r => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(r.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && r.CreatedAt.Year == d.Year),
                    "year" => rooms.Count(r => r.CreatedAt.Year == d.Year),
                    _ => rooms.Count(r => r.CreatedAt.Year == d.Year && r.CreatedAt.Month == d.Month)
                }
            }).ToList();
            return Ok(new {
                userGrowth = new { labels = userGrowth.Select(x => x.Label), data = userGrowth.Select(x => x.Count) },
                adPostGrowth = new { labels = adPostGrowth.Select(x => x.Label), data = adPostGrowth.Select(x => x.Count) },
                roomGrowth = new { labels = roomGrowth.Select(x => x.Label), data = roomGrowth.Select(x => x.Count) }
            });
        }

        [HttpGet("roles")]
        public IActionResult Roles()
        {
            var users = _userManager.Users.ToList();
            var roles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
            var roleLabels = roles.Select(r => r.ToString()).ToList();
            var roleCounts = roles.Select(r => users.Count(u => u.Role == r)).ToList();
            return Ok(new {
                labels = roleLabels,
                data = roleCounts
            });
        }
    }
}
