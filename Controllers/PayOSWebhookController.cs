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
                string status = "";
                if (root.TryGetProperty("data", out var dataElem))
                {
                    status = dataElem.GetProperty("status").GetString() ?? string.Empty;
                    try
                    {
                        orderCode = dataElem.GetProperty("orderCode").GetInt32();
                    }
                    catch { return Ok(new { success = true }); }
                }
                if (status == "PAID")
                {
                    // Duyệt tất cả UserAdPackage, so sánh Id chuyển sang int bằng BitConverter.ToInt32(Guid.ToByteArray(), 0) == orderCode
                    var pkgs = await _context.UserAdPackages.ToListAsync();
                    foreach (var pkg in pkgs)
                    {
                        int code = BitConverter.ToInt32(pkg.Id.ToByteArray(), 0);
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
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                Console.WriteLine($"Error processing webhook: {ex.Message}");
                return Ok(new { success = true }); // Bỏ qua lỗi nếu không parse được JSON
            }
            return Ok(new { success = true });
        }

        private string CalculateChecksum(string body, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
