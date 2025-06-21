using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RentalService.Data;
using RentalService.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace RentalService.Controllers
{
    [ApiController]
    [Route("api/payos/webhook")]
    public class PayOSWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public PayOSWebhookController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }
            var checksumKey = _config["PayOS:ChecksumKey"];
            var receivedChecksum = Request.Headers["x-checksum"].ToString();
            var calculatedChecksum = CalculateChecksum(body, checksumKey);
            if (receivedChecksum != calculatedChecksum)
                return Unauthorized();

            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var orderId = root.GetProperty("data").GetProperty("orderId").GetString();
            var status = root.GetProperty("data").GetProperty("status").GetString();
            // Xử lý trạng thái thanh toán
            if (status == "PAID")
            {
                // Tìm UserAdPackage theo orderId (nếu đã lưu khi tạo payment)
                var pkg = await _context.UserAdPackages.FirstOrDefaultAsync(x => x.Id.ToString() == orderId);
                if (pkg != null)
                {
                    pkg.IsActive = true;
                    pkg.PurchaseDate = DateTime.UtcNow;
                    // Cập nhật ExpiryDate, RemainingPosts nếu cần
                    await _context.SaveChangesAsync();
                }
            }
            return Ok();
        }

        private string CalculateChecksum(string body, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
