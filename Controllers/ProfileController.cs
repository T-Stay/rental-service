using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProfileController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                .Include(u => u.ContactInformations)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound();
            // Lấy UserAdPackages trực tiếp từ context theo UserId
            var userAdPackages = await _context.UserAdPackages
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
            user.UserAdPackages = userAdPackages;
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddContact(ContactType type, string data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null) return NotFound();
            // Basic validation
            if (type == ContactType.Email && !data.Contains("@")) ModelState.AddModelError("data", "Invalid email");
            if (type == ContactType.PhoneNumber && data.Length < 8) ModelState.AddModelError("data", "Invalid phone number");
            if (type == ContactType.PhoneNumber)
            {
                // Kiểm tra OTP đã xác thực chưa
                var otpEntity = await _context.PhoneOtps
                    .Where(x => x.PhoneNumber == data && x.IsUsed && x.ExpiredAt > DateTime.UtcNow)
                    .OrderByDescending(x => x.LastSentAt)
                    .FirstOrDefaultAsync();
                if (otpEntity == null)
                {
                    ModelState.AddModelError("data", "Bạn cần xác thực OTP trước khi thêm số điện thoại.");
                    return RedirectToAction("Index");
                }
            }
            if (!ModelState.IsValid) return RedirectToAction("Index");
            var contact = new ContactInformation { Id = Guid.NewGuid(), UserId = user.Id, Type = type, Data = data, CreatedAt = DateTime.UtcNow, IsVerified = (type == ContactType.PhoneNumber) };
            _context.ContactInformations.Add(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContact(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contact = await _context.ContactInformations.FirstOrDefaultAsync(c => c.Id == id && c.UserId.ToString() == userId);
            if (contact != null)
            {
                _context.ContactInformations.Remove(contact);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId.ToString() == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(12)
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("g")
                })
                .ToListAsync();
            return Json(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> SendPhoneOtp(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 8)
                return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
            // Chuẩn hóa số điện thoại Việt Nam về E.164
            string normalizedPhone = phone;
            if (phone.StartsWith("0") && phone.Length == 10)
                normalizedPhone = "+84" + phone.Substring(1);
            else if (phone.StartsWith("+84") && phone.Length == 12) // đã đúng
                normalizedPhone = phone;
            else if (phone.StartsWith("84") && phone.Length == 11)
                normalizedPhone = "+" + phone;
            // ...có thể bổ sung thêm logic cho các quốc gia khác nếu cần
            var now = DateTime.UtcNow;
            // Cleanup OTP cũ
            var oldOtps = _context.PhoneOtps.Where(x => x.ExpiredAt < now || x.IsUsed);
            _context.PhoneOtps.RemoveRange(oldOtps);
            await _context.SaveChangesAsync();
            var lastOtp = await _context.PhoneOtps.OrderByDescending(x => x.LastSentAt)
                .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone && !x.IsUsed && x.ExpiredAt > now);
            if (lastOtp != null && (now - lastOtp.LastSentAt).TotalSeconds < 30)
                return Json(new { success = false, message = "Vui lòng chờ 30 giây trước khi gửi lại mã." });
            var otp = new Random().Next(100000, 999999).ToString();
            var otpEntity = new PhoneOtp
            {
                Id = Guid.NewGuid(),
                PhoneNumber = normalizedPhone,
                Otp = otp,
                ExpiredAt = now.AddMinutes(5),
                LastSentAt = now,
                IsUsed = false
            };
            _context.PhoneOtps.Add(otpEntity);
            await _context.SaveChangesAsync();
            // Gửi OTP qua Zalo ZNS (tích hợp tại đây)
            // TODO: Call Zalo ZNS API to send OTP to normalizedPhone
            // Example: await _zaloZnsService.SendOtpAsync(normalizedPhone, otp);
            // Hiện tại chỉ trả về thành công giả lập
            return Json(new { success = true, message = "Đã gửi mã OTP qua Zalo ZNS (giả lập)" });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPhoneOtp(string phone, string otp)
        {
            var now = DateTime.UtcNow;
            var otpEntity = await _context.PhoneOtps
                .Where(x => x.PhoneNumber == phone && !x.IsUsed && x.ExpiredAt > now)
                .OrderByDescending(x => x.LastSentAt)
                .FirstOrDefaultAsync();
            if (otpEntity == null || otpEntity.Otp != otp)
                return Json(new { success = false, message = "Mã OTP không đúng hoặc đã hết hạn" });
            otpEntity.IsUsed = true;
            // Đánh dấu số điện thoại đã xác thực nếu đã tồn tại trong ContactInformation
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contact = await _context.ContactInformations.FirstOrDefaultAsync(c => c.UserId.ToString() == userId && c.Type == ContactType.PhoneNumber && c.Data == phone);
            if (contact != null)
            {
                contact.IsVerified = true;
                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Xác thực thành công" });
        }
    }
}
