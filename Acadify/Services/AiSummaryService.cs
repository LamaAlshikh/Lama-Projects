using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Acadify.Services
{
    public class AiAcademicAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiAcademicAgentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// هذه الدالة تعمل كـ Agent: تأخذ بيانات الطالب والمواد المتاحة وتقترح جدولاً
        /// </summary>
        public async Task<string> SuggestScheduleAsync(string studentTranscript, string availableCourses)
        {
            var prompt = $"""
بناءً على السجل الأكاديمي للطالب:
{studentTranscript}

وقائمة المواد المطروحة للفصل القادم:
{availableCourses}

قم بدور المستشار الأكاديمي لمشروع (Acadify) ونفذ المهام التالية:
1. فحص المواد المتبقية (Degree Audit).
2. التأكد من فتح المتطلبات السابقة (Prerequisites).
3. اقتراح أفضل 5 مواد لتسجيلها مع ذكر السبب لكل مادة.
""";

            return await GetRawResponseAsync(
                prompt,
                "أنت مستشار أكاديمي خبير في أنظمة الجامعات، تساعد الطلاب في اختيار أفضل المواد بناءً على سجلهم الأكاديمي.",
                1000);
        }

        public async Task<string> GetRawResponseAsync(string prompt, string instructions, int maxOutputTokens = 800)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "";

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini"; // يفضل استخدام gpt-4o-mini لأنه أسرع وأرخص

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("OpenAI API key not found in configuration.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            // الهيكل الصحيح لـ OpenAI Chat Completions API
            var body = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = instructions },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                max_tokens = maxOutputTokens
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // الرابط الرسمي لـ OpenAI
            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI API error: {response.StatusCode} - {responseText}");

            using var doc = JsonDocument.Parse(responseText);

            // استخراج النص من الهيكل الرسمي لـ OpenAI
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString()?.Trim() ?? "";
                }
            }

            throw new Exception("OpenAI response did not contain the expected text structure.");
        }

        public async Task<string> SummarizeMeetingChatAsync(string chatRecord)
        {
            if (string.IsNullOrWhiteSpace(chatRecord))
                return "";

            var prompt = $"""
لخص المحادثة التالية باللغة العربية الفصحى، بصياغة رسمية ومختصرة مناسبة تمامًا لوضعها في خانة:
"Proposed Solutions / Advise / Brief notes"

القواعد:
- اكتب الملخص في فقرة قصيرة من سطر إلى 3 أسطر.
- ركز فقط على:
  1) موضوع النقاش الأساسي
  2) التوجيه أو النصيحة التي قدمتها المرشدة
  3) القرار أو التوصية النهائية إن وجدت
- تجاهل التحية والكلام الجانبي والتكرار.
- لا تكتب "Student:" أو "Advisor:".
- لا تنقل الحوار حرفيًا إلا عند الضرورة.
- لا تستخدم تعداد نقطي.
- الناتج النهائي يكون بالعربية فقط.

المحادثة:
{chatRecord}
""";

            return await GetRawResponseAsync(
                prompt,
                "أنت مساعد متخصص في تلخيص محادثات الإرشاد الأكاديمي بصياغة عربية رسمية وواضحة.",
                250);
        }
    }
}