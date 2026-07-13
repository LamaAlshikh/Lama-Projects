using Acadify.Models;
using Acadify.Models.Db;

namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IAcademicCalendarAiExtractor
    {
        Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string filePath, int calendarId);
    }
}