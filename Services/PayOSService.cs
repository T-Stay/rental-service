using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RentalService.Services
{
    public class PayOSService
    {
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _returnUrl;
        private readonly string _webhookUrl;
        private readonly HttpClient _httpClient;

        public PayOSService(IConfiguration config)
        {
            var section = config.GetSection("PayOS");
            _clientId = section["ClientId"] ?? string.Empty;
            _apiKey = section["ApiKey"] ?? string.Empty;
            _checksumKey = section["ChecksumKey"] ?? string.Empty;
            _returnUrl = section["ReturnUrl"] ?? string.Empty;
            _webhookUrl = section["WebhookUrl"] ?? string.Empty;
            _httpClient = new HttpClient();
        }

        public PayOSService(string clientId, string apiKey, string checksumKey, string returnUrl, string webhookUrl)
        {
            _clientId = clientId;
            _apiKey = apiKey;
            _checksumKey = checksumKey;
            _returnUrl = returnUrl;
            _webhookUrl = webhookUrl;
            _httpClient = new HttpClient();
        }

        public async Task<string> CreatePaymentLinkAsync(string orderUuid, string description, long amount, string cancelUrl, string? buyerName = null, string? buyerEmail = null, string? buyerPhone = null, string? buyerAddress = null)
        {
            // PayOS chỉ nhận orderCode là int, nên hash UUID thành int (ví dụ: lấy 8 byte đầu của Guid làm long, cast về int, hoặc dùng GetHashCode)
            int orderCode = orderUuid.GetHashCode();
            if (orderCode < 0) orderCode = -orderCode; // PayOS yêu cầu số dương

            // Tạo signature đúng chuẩn PayOS
            string signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={_returnUrl}";
            string signature = CreateSignature(signatureData, _checksumKey);

            var payload = new
            {
                orderCode = orderCode,
                amount = amount,
                description = description,
                cancelUrl = cancelUrl,
                returnUrl = _returnUrl,
                signature = signature,
                buyerName = buyerName,
                buyerEmail = buyerEmail,
                buyerPhone = buyerPhone,
                buyerAddress = buyerAddress
            };
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            var json = JsonSerializer.Serialize(payload, options);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
            request.Headers.Add("x-client-id", _clientId);
            request.Headers.Add("x-api-key", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var payUrl = doc.RootElement.GetProperty("data").GetProperty("checkoutUrl").GetString() ?? string.Empty;
            return payUrl;
        }

        private static string CreateSignature(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }
    }
}
