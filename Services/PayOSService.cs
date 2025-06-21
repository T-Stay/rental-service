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
            // PayOS chỉ nhận orderCode là int, nên hash UUID thành int (dễ bị tràn số hoặc không khớp do thuật toán GetHashCode khác nhau giữa môi trường)
            // Để đảm bảo orderCode gửi lên và trả về giống nhau, hãy chuyển Guid sang int một cách ổn định (ví dụ: lấy 4 byte đầu của Guid)
            int orderCode = BitConverter.ToInt32(Guid.Parse(orderUuid).ToByteArray(), 0);
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
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // Nếu lỗi "Đơn thanh toán đã tồn tại", thử lấy lại link thanh toán cũ
                try
                {
                    var errorDoc = JsonDocument.Parse(responseBody);
                    if (errorDoc.RootElement.TryGetProperty("code", out var codeElem) && codeElem.GetString() == "231")
                    {
                        // Gọi API lấy lại link thanh toán cũ
                        var getReq = new HttpRequestMessage(HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}");
                        getReq.Headers.Add("x-client-id", _clientId);
                        getReq.Headers.Add("x-api-key", _apiKey);
                        var getResp = await _httpClient.SendAsync(getReq);
                        var getBody = await getResp.Content.ReadAsStringAsync();
                        using var getDoc = JsonDocument.Parse(getBody);
                        if (getDoc.RootElement.TryGetProperty("data", out var dataElem2) && dataElem2.TryGetProperty("checkoutUrl", out var urlElem2))
                        {
                            var payUrl = urlElem2.GetString() ?? string.Empty;
                            return payUrl;
                        }
                        throw new Exception("Không lấy được checkoutUrl từ đơn đã tồn tại: " + getBody);
                    }
                }
                catch { /* ignore, sẽ throw lỗi phía dưới */ }
                throw new Exception($"PayOS trả về lỗi: {responseBody}");
            }
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.TryGetProperty("checkoutUrl", out var urlElem))
            {
                var payUrl = urlElem.GetString() ?? string.Empty;
                return payUrl;
            }
            throw new Exception("Không nhận được checkoutUrl từ PayOS: " + responseBody);
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
