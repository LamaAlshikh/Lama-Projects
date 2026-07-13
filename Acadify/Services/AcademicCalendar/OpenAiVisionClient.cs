using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Acadify.Services.AcademicCalendar
{
    public class OpenAiVisionClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public OpenAiVisionClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<string> GetJsonFromImagesAsync(string prompt, List<byte[]> pngImages)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("OpenAI:ApiKey is missing in appsettings.json");

            var model = _config["OpenAI:Model"] ?? "gpt-4.1-mini";

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var contentList = new List<object>
            {
                new
                {
                    type = "input_text",
                    text = prompt
                }
            };

            foreach (var img in pngImages)
            {
                var b64 = Convert.ToBase64String(img);

                contentList.Add(new
                {
                    type = "input_image",
                    image_url = $"data:image/png;base64,{b64}"
                });
            }

            var body = new
            {
                model = model,
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = contentList
                    }
                },
                temperature = 0
            };

            var json = JsonSerializer.Serialize(body);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync("https://api.openai.com/v1/responses", httpContent);
            var resText = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"OpenAI error: {res.StatusCode} - {resText}");

            using var doc = JsonDocument.Parse(resText);

            if (!doc.RootElement.TryGetProperty("output", out var output))
                throw new Exception("OpenAI response does not contain 'output'.");

            foreach (var item in output.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "message" &&
                    item.TryGetProperty("content", out var content))
                {
                    foreach (var c in content.EnumerateArray())
                    {
                        if (c.TryGetProperty("type", out var cType) &&
                            cType.GetString() == "output_text" &&
                            c.TryGetProperty("text", out var txt))
                        {
                            return txt.GetString() ?? "";
                        }
                    }
                }
            }

            throw new Exception("No output_text returned from OpenAI.");
        }
    }
}