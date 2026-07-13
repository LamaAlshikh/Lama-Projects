using System.Globalization;
using Acadify.Models.Db;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class AcademicCalendarFixedExtractor : IAcademicCalendarAiExtractor
    {
        private static readonly Dictionary<string, string> FixedEvents = new()
        {
            { "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS", "2025-08-18" },
            { "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS", "2025-08-30" },

            { "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات", "2025-08-19" },
            { "نهاية فترة التسجيل للمرشدين الأكاديميين", "2025-09-05" },

            { "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي", "2025-09-28" },
            { "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي", "2025-11-06" },

            { "بداية تقديم طلبات التأجيل", "2025-08-31" },
            { "نهاية تقديم طلبات التأجيل", "2026-01-01" },

            { "بداية تقديم طلبات الاعتذار", "2025-09-14" },
            { "نهاية فترة تقديم طلبات الاعتذار", "2025-11-20" }
        };

        public Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId)
        {
            var result = new List<AcademicCalendarEvent>();

            foreach (var item in FixedEvents)
            {
                var date = DateTime.ParseExact(
                    item.Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);

                result.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = item.Key,
                    GregorianDate = date,
                    HijriDate = "-",
                    DayAr = null
                });
            }

            return Task.FromResult(result);
        }
    }
}