using Acadify.Models;
using Acadify.Models.Db;
using Acadify.Services.AcademicCalendar.Interfaces;
using ClosedXML.Excel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Acadify.Services.AcademicCalendar
{
    public class AcademicCalendarAiExtractor : IAcademicCalendarAiExtractor
    {
        private sealed class EventSpec
        {
            public string EventName { get; set; } = "";
            public bool UseEndDate { get; set; }
            public List<string> Aliases { get; set; } = new();
        }

        private sealed class CalendarRow
        {
            public string EventText { get; set; } = "";
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        private static readonly List<EventSpec> TargetEvents = new()
        {
            new EventSpec
            {
                EventName = "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
                UseEndDate = false,
                Aliases = new()
                {
                    "تسجيل المقررات للطلاب والطالبات على odus plus",
                    "تسجيل المقررات للطالب والطالبات على odus plus",
                    "registration for male and female students on odus plus",
                    "students registration on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
                UseEndDate = true,
                Aliases = new()
                {
                    "تسجيل المقررات للطلاب والطالبات على odus plus",
                    "تسجيل المقررات للطالب والطالبات على odus plus",
                    "registration for male and female students on odus plus",
                    "students registration on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات",
                UseEndDate = false,
                Aliases = new()
                {
                    "تسجيل المقررات للمرشدين الأكاديميين",
                    "تسجيل المقررات للشؤون التعليمية بالكليات",
                    "تسجيل المقررات للوكلاء والوكيلات بالكليات",
                    "academic advisors registration",
                    "registration for academic advisors",
                    "registration for academic affairs in colleges",
                    "registration for vice deans and female vice deans in colleges"
                }
            },
            new EventSpec
            {
                EventName = "نهاية فترة التسجيل للمرشدين الأكاديميين",
                UseEndDate = true,
                Aliases = new()
                {
                    "تسجيل المقررات للمرشدين الأكاديميين",
                    "academic advisors registration",
                    "registration for academic advisors"
                }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي",
                UseEndDate = false,
                Aliases = new()
                {
                    "تقديم طلبات سحب مقرر للفصل الحالي على odus plus",
                    "course withdrawal requests for the current semester on odus plus",
                    "withdrawal requests on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي",
                UseEndDate = true,
                Aliases = new()
                {
                    "تقديم طلبات سحب مقرر للفصل الحالي على odus plus",
                    "course withdrawal requests for the current semester on odus plus",
                    "withdrawal requests on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات التأجيل",
                UseEndDate = false,
                Aliases = new()
                {
                    "تقديم طلبات تأجيل الفصل الدراسي الأول للعام الجامعي 1448/1449هـ على odus plus",
                    "deferment requests on odus plus",
                    "postponement requests on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "نهاية تقديم طلبات التأجيل",
                UseEndDate = true,
                Aliases = new()
                {
                    "تقديم طلبات تأجيل الفصل الدراسي الأول للعام الجامعي 1448/1449هـ على odus plus",
                    "deferment requests on odus plus",
                    "postponement requests on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات الاعتذار",
                UseEndDate = false,
                Aliases = new()
                {
                    "تقديم طلبات الاعتذار عن الفصل الحالي على odus plus",
                    "apology requests for the current semester on odus plus",
                    "withdrawal from semester requests on odus plus"
                }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تقديم طلبات الاعتذار",
                UseEndDate = true,
                Aliases = new()
                {
                    "تقديم طلبات الاعتذار عن الفصل الحالي على odus plus",
                    "apology requests for the current semester on odus plus",
                    "withdrawal from semester requests on odus plus"
                }
            }
        };

        public Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string filePath, int calendarId)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext != ".xlsx")
                throw new InvalidOperationException("Please upload the academic calendar as an Excel (.xlsx) file.");

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.First();

            var rows = ReadCalendarRows(worksheet);
            var results = new List<AcademicCalendarEvent>();

            // أولاً: الأحداث الأساسية المعروفة
            foreach (var spec in TargetEvents)
            {
                var matchedRow = FindBestRow(rows, spec);
                if (matchedRow == null)
                    continue;

                var selectedDate = spec.UseEndDate
                    ? (matchedRow.ToDate ?? matchedRow.FromDate)
                    : (matchedRow.FromDate ?? matchedRow.ToDate);

                if (selectedDate == null)
                    continue;

                results.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = spec.EventName,
                    GregorianDate = selectedDate.Value.Date,
                    HijriDate = "-",
                    DayAr = null
                });
            }

            // ثانيًا: أي صف فيه كلمة إجازة ينحفظ كما هو
            foreach (var row in rows)
            {
                if (!ContainsVacationWord(row.EventText))
                    continue;

                var selectedDate = row.FromDate ?? row.ToDate;
                if (selectedDate == null)
                    continue;

                var eventName = row.EventText.Trim();

                bool alreadyExists = results.Any(x =>
                    Normalize(x.EventName) == Normalize(eventName) &&
                    x.GregorianDate.Date == selectedDate.Value.Date);

                if (alreadyExists)
                    continue;

                results.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = eventName,
                    GregorianDate = selectedDate.Value.Date,
                    HijriDate = "-",
                    DayAr = null
                });
            }

            return Task.FromResult(results);
        }

        private List<CalendarRow> ReadCalendarRows(IXLWorksheet ws)
        {
            var rows = new List<CalendarRow>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

            for (int row = 1; row <= lastRow; row++)
            {
                // حسب ملفك:
                // A = الحدث
                // B = من / اليوم والتاريخ
                // D = إلى / اليوم والتاريخ

                var eventText = ws.Cell(row, 1).GetString().Trim();
                var fromText = ws.Cell(row, 2).GetString().Trim();
                var toText = ws.Cell(row, 4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(eventText))
                    continue;

                if (IsHeaderLike(eventText))
                    continue;

                var fromDates = ExtractGregorianDates(fromText);
                var toDates = ExtractGregorianDates(toText);

                DateTime? fromDate = fromDates.FirstOrDefault();
                DateTime? toDate = toDates.FirstOrDefault();

                if (fromDate == null && toDate == null)
                    continue;

                rows.Add(new CalendarRow
                {
                    EventText = eventText,
                    FromDate = fromDate,
                    ToDate = toDate
                });
            }

            return rows;
        }

        private CalendarRow? FindBestRow(List<CalendarRow> rows, EventSpec spec)
        {
            CalendarRow? bestRow = null;
            int bestScore = 0;

            foreach (var row in rows)
            {
                // نتجنب صفوف الإجازات عند مطابقة الأحداث الأساسية
                if (ContainsVacationWord(row.EventText))
                    continue;

                var score = ScoreEvent(row.EventText, spec);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestRow = row;
                }
            }

            return bestScore >= 55 ? bestRow : null;
        }

        private int ScoreEvent(string candidate, EventSpec spec)
        {
            var normalizedCandidate = Normalize(candidate);
            int best = 0;

            foreach (var alias in spec.Aliases.Append(spec.EventName))
            {
                var normalizedAlias = Normalize(alias);

                if (normalizedCandidate.Contains(normalizedAlias) || normalizedAlias.Contains(normalizedCandidate))
                    best = Math.Max(best, 100);

                var words = normalizedAlias
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2)
                    .Distinct()
                    .ToList();

                if (words.Count == 0)
                    continue;

                int matched = words.Count(w => normalizedCandidate.Contains(w));
                int score = (matched * 100) / words.Count;
                best = Math.Max(best, score);
            }

            return best;
        }

        private List<DateTime> ExtractGregorianDates(string input)
        {
            var results = new List<DateTime>();

            if (string.IsNullOrWhiteSpace(input))
                return results;

            var text = input.Trim();

            var ymdMatches = Regex.Matches(text, @"(20\d{2})-(\d{2})-(\d{2})");
            foreach (Match m in ymdMatches)
            {
                var value = $"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}";
                if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    results.Add(dt.Date);
            }

            var dmySlashMatches = Regex.Matches(text, @"(\d{2})/(\d{2})/(20\d{2})");
            foreach (Match m in dmySlashMatches)
            {
                var value = $"{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}";
                if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    results.Add(dt.Date);
            }

            return results.Distinct().OrderBy(x => x).ToList();
        }

        private bool ContainsVacationWord(string text)
        {
            var normalized = Normalize(text);
            return normalized.Contains(Normalize("إجازة")) ||
                   normalized.Contains(Normalize("اجازة"));
        }

        private bool IsHeaderLike(string text)
        {
            var normalized = Normalize(text);

            return normalized == Normalize("الحدث") ||
                   normalized == Normalize("من") ||
                   normalized == Normalize("إلى") ||
                   normalized.Contains(Normalize("اليوم والتاريخ")) ||
                   normalized.Contains(Normalize("الأسبوع"));
        }

        private string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.ToLowerInvariant();

            text = text.Replace("أ", "ا")
                       .Replace("إ", "ا")
                       .Replace("آ", "ا")
                       .Replace("ى", "ي")
                       .Replace("ة", "ه")
                       .Replace("ؤ", "و")
                       .Replace("ئ", "ي")
                       .Replace("ـ", "");

            text = Regex.Replace(text, @"\([a-z]\d+\)", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"[^\w\s/-]", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}