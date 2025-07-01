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

        // Dashboard - hiển thị số liệu thống kê tổng quan
        public IActionResult Index()
        {
            var totalUsers = _userManager.Users.Count();
            var totalBuildings = _context.Buildings.Count();
            var totalRooms = _context.Rooms.Count();
            var totalPendingAds = _context.AdPosts.Count(a => !a.IsActive);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBuildings = totalBuildings;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.PendingAds = totalPendingAds;
            return View();
        }

        // Quản lý người dùng
        public IActionResult UserList()
        {
            var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
            return View(users);
        }

        // Thống kê hệ thống (chi tiết, truyền dữ liệu cho biểu đồ, hỗ trợ filter thời gian)
        public IActionResult Statistics(string unit = "month", int range = 12)
        {
            var totalUsers = _userManager.Users.Count();
            var totalRooms = _context.Rooms.Count();
            var totalBuildings = _context.Buildings.Count();
            var totalAdPosts = _context.AdPosts.Count();
            var totalPendingAds = _context.AdPosts.Count(a => !a.IsActive);
            var totalPendingRooms = _context.Rooms.Count(r => r.Status == RoomStatus.Inactive);

            // Xác định bước thời gian
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
            // Tạo các mốc thời gian
            var timePoints = Enumerable.Range(0, range)
                .Select(i => {
                    var d = now;
                    for (int j = 0; j < i; j++) d = stepBack(d);
                    return d;
                })
                .Reverse()
                .ToList();

            // Tăng trưởng người dùng
            var userGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => _userManager.Users.Count(u => u.CreatedAt.Date == d.Date),
                    "week" => _userManager.Users.Count(u => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(u.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && u.CreatedAt.Year == d.Year),
                    "year" => _userManager.Users.Count(u => u.CreatedAt.Year == d.Year),
                    _ => _userManager.Users.Count(u => u.CreatedAt.Year == d.Year && u.CreatedAt.Month == d.Month)
                }
            }).ToList();
            ViewBag.UserGrowthData = System.Text.Json.JsonSerializer.Serialize(new {
                labels = userGrowth.Select(x => x.Label),
                data = userGrowth.Select(x => x.Count)
            });

            // Tỉ lệ vai trò người dùng
            var roles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
            var roleLabels = roles.Select(r => r.ToString()).ToList();
            var roleCounts = roles.Select(r => _userManager.Users.Count(u => u.Role == r)).ToList();
            ViewBag.UserRoleData = System.Text.Json.JsonSerializer.Serialize(new {
                labels = roleLabels,
                data = roleCounts
            });

            // Số lượng bài quảng cáo theo thời gian
            var adPostGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => _context.AdPosts.Count(a => a.CreatedAt.Date == d.Date),
                    "week" => _context.AdPosts.Count(a => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(a.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && a.CreatedAt.Year == d.Year),
                    "year" => _context.AdPosts.Count(a => a.CreatedAt.Year == d.Year),
                    _ => _context.AdPosts.Count(a => a.CreatedAt.Year == d.Year && a.CreatedAt.Month == d.Month)
                }
            }).ToList();
            ViewBag.AdPostData = System.Text.Json.JsonSerializer.Serialize(new {
                labels = adPostGrowth.Select(x => x.Label),
                data = adPostGrowth.Select(x => x.Count)
            });

            // Số lượng phòng mới theo thời gian
            var roomGrowth = timePoints.Select(d => new {
                Label = labelFormat(d),
                Count = unit switch
                {
                    "day" => _context.Rooms.Count(r => r.CreatedAt.Date == d.Date),
                    "week" => _context.Rooms.Count(r => System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(r.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) && r.CreatedAt.Year == d.Year),
                    "year" => _context.Rooms.Count(r => r.CreatedAt.Year == d.Year),
                    _ => _context.Rooms.Count(r => r.CreatedAt.Year == d.Year && r.CreatedAt.Month == d.Month)
                }
            }).ToList();
            ViewBag.RoomGrowthData = System.Text.Json.JsonSerializer.Serialize(new {
                labels = roomGrowth.Select(x => x.Label),
                data = roomGrowth.Select(x => x.Count)
            });

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.TotalBuildings = totalBuildings;
            ViewBag.TotalAdPosts = totalAdPosts;
            ViewBag.PendingAds = totalPendingAds;
            ViewBag.PendingRooms = totalPendingRooms;
            return View();
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

        // GET: /Admin/Details/{id}
        public IActionResult Details(Guid id)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: /Admin/Edit/{id}
        public IActionResult Edit(Guid id)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Admin/Edit/{id}
        [HttpPost]
        public IActionResult Edit(Guid id, string name, string email)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            user.Name = name;
            user.Email = email;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: /Admin/Lock/{id}
        public IActionResult Lock(Guid id)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // API: Thống kê số lượng gói được mua theo thời gian
        [HttpGet]
        [Route("api/admin/statistics/adpackages/growth")]
        public IActionResult AdPackageGrowth(string unit = "month", int range = 12)
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
                .Select(i => { var d = now; for (int j = 0; j < i; j++) d = stepBack(d); return d; })
                .Reverse().ToList();
            var types = Enum.GetValues(typeof(AdPackageType)).Cast<AdPackageType>().ToList();
            var labels = timePoints.Select(d => labelFormat(d)).ToList();

            // Lấy toàn bộ UserAdPackages về memory để xử lý tuần
            var allPackages = _context.UserAdPackages.AsNoTracking().ToList();
            var datasets = types.Select(type => new {
                label = type.ToVietnameseLabel(),
                type = type,
                data = timePoints.Select(d =>
                {
                    switch (unit)
                    {
                        case "day":
                            return allPackages.Count(p => p.PackageType == type && p.PurchaseDate.Date == d.Date);
                        case "week":
                            var week = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                            return allPackages.Count(p => p.PackageType == type &&
                                System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(p.PurchaseDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) == week &&
                                p.PurchaseDate.Year == d.Year);
                        case "year":
                            return allPackages.Count(p => p.PackageType == type && p.PurchaseDate.Year == d.Year);
                        default:
                            return allPackages.Count(p => p.PackageType == type && p.PurchaseDate.Year == d.Year && p.PurchaseDate.Month == d.Month);
                    }
                }).ToList()
            }).ToList();
            return Json(new { labels, datasets });
        }

        // API: Tỉ lệ các loại gói đã được mua
        [HttpGet]
        [Route("api/admin/statistics/adpackages/ratio")]
        public IActionResult AdPackageRatio()
        {
            var types = Enum.GetValues(typeof(AdPackageType)).Cast<AdPackageType>().ToList();
            var labels = types.Select(t => t.ToVietnameseLabel()).ToList();
            var data = types.Select(t => _context.UserAdPackages.Count(p => p.PackageType == t)).ToList();
            return Json(new { labels, data });
        }

        // Add more actions for role management as needed
    }
}
