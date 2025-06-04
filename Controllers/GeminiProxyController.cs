using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace RentalService.Controllers
{
    [ApiController]
    [Route("api/gemini-proxy")]
    public class GeminiProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string GeminiApiKey;
        private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:streamGenerateContent?alt=sse&key=";

        public GeminiProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            GeminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        }

        [HttpPost]
        public async Task Proxy()
        {
            // Đọc body từ client
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, GeminiApiUrl + GeminiApiKey)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = "text/event-stream";

            using var responseStream = await response.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(Response.Body);
        }
    }
}
