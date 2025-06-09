using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            _clientId = section["ClientId"];
            _apiKey = section["ApiKey"];
            _checksumKey = section["ChecksumKey"];
            _returnUrl = section["ReturnUrl"];
            _webhookUrl = section["WebhookUrl"];
            _httpClient = new HttpClient();
        }

        public async Task<string> CreatePaymentLinkAsync(string orderId, string description, long amount, string userId)
        {
            var payload = new
            {
                orderId = orderId,
                amount = amount,
                description = description,
                returnUrl = _returnUrl,
                webhookUrl = _webhookUrl,
                buyerId = userId
            };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.payos.vn/v1/payment-requests");
            request.Headers.Add("x-client-id", _clientId);
            request.Headers.Add("x-api-key", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var payUrl = doc.RootElement.GetProperty("data").GetProperty("checkoutUrl").GetString();
            return payUrl;
        }
    }
}
