using Acadify.Models.Db;

namespace Acadify.Services.AcademicCalendar
{
    public static class AcademicCalendarValidator
    {
        private static readonly HashSet<string> AllowedEvents = new()
        {
            "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات",
            "نهاية فترة التسجيل للمرشدين الأكاديميين",
            "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي",
            "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي",
            "بداية تقديم طلبات التأجيل",
            "نهاية تقديم طلبات التأجيل",
            "بداية تقديم طلبات الاعتذار",
            "نهاية فترة تقديم طلبات الاعتذار",
            "إجازة نهاية العام"
        };

        public static void ValidateOrThrow(List<AcademicCalendarEvent>? events)
        {
            if (events == null || events.Count == 0)
                throw new InvalidOperationException("No academic calendar events were extracted.");

            foreach (var e in events)
            {
                if (string.IsNullOrWhiteSpace(e.EventName))
                    throw new InvalidOperationException("An extracted event has an empty name.");

                if (!AllowedEvents.Contains(e.EventName))
                    throw new InvalidOperationException($"Unexpected event: {e.EventName}");

                if (e.GregorianDate == default)
                    throw new InvalidOperationException($"Invalid gregorian date for event: {e.EventName}");

                if (!IsValidDay(e.DayAr))
                    throw new InvalidOperationException($"Invalid day_ar: {e.DayAr}");
            }

            var duplicatedEvents = events
                .GroupBy(e => e.EventName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedEvents.Any())
                throw new InvalidOperationException(
                    "Duplicate event names were found: " + string.Join(", ", duplicatedEvents));
        }

        private static bool IsValidDay(string? day)
        {
            if (string.IsNullOrWhiteSpace(day))
                return true;

            return new[]
            {
                "الأحد",
                "الاثنين",
                "الثلاثاء",
                "الأربعاء",
                "الخميس",
                "الجمعة",
                "السبت"
            }.Contains(day.Trim());
        }
    }
}
