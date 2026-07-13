using System.Text.Json.Serialization;

namespace Acadify.Models.AdminPages
{
    public class AcademicCalendarAiEvent
    {
        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = "";

        // Gregorian فقط (dd/MM/yyyy) أو null
        [JsonPropertyName("gregorianDate")]
        public string? GregorianDate { get; set; }
    }
}