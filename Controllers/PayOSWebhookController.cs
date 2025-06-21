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
            // BỎ QUA CHECKSUM ĐỂ TEST
            // var checksumKey = _config["PayOS:ChecksumKey"] ?? string.Empty;
            // var receivedChecksum = Request.Headers["x-checksum"].ToString();
            // var calculatedChecksum = CalculateChecksum(body, checksumKey);
            // if (receivedChecksum != calculatedChecksum)
            //     return Unauthorized();

            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                // Lấy orderCode từ webhook
                int orderCode = 0;
                string status = root.GetProperty("data").GetProperty("status").GetString() ?? string.Empty;
                try
                {
                    orderCode = root.GetProperty("data").GetProperty("orderCode").GetInt32();
                }
                catch { return Ok(); }
                if (status == "PAID")
                {
                    // Duyệt tất cả UserAdPackage, so sánh Id.GetHashCode() == orderCode
                    var pkgs = await _context.UserAdPackages.ToListAsync();
                    foreach (var pkg in pkgs)
                    {
                        int code = pkg.Id.GetHashCode();
                        if (code < 0) code = -code;
                        if (code == orderCode)
                        {
                            pkg.IsActive = true;
                            pkg.PurchaseDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            break;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return Ok(); // Bỏ qua lỗi nếu không parse được JSON
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
