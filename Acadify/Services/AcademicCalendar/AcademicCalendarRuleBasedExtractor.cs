using System.Globalization;
using System.Text.RegularExpressions;
using Acadify.Models.Db;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class AcademicCalendarRuleBasedExtractor : IAcademicCalendarAiExtractor
    {
        private readonly IPdfTextExtractor _pdfText;
        private readonly IPdfOcrService _ocr;

        public AcademicCalendarRuleBasedExtractor(IPdfTextExtractor pdfText, IPdfOcrService ocr)
        {
            _pdfText = pdfText;
            _ocr = ocr;
        }

        private sealed class EventSpec
        {
            public string EventName { get; set; } = "";
            public List<string> Aliases { get; set; } = new();
        }

        private sealed class Segment
        {
            public DateTime Gregorian { get; set; }
            public string Text { get; set; } = "";
        }

        private static readonly List<EventSpec> TargetEvents = new()
        {
            new EventSpec
            {
                EventName = "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
                Aliases = new() { "بداية فترة تسجيل المقررات للطالب", "بداية تسجيل المقررات للطالب", "تسجيل المقررات للطالب والطالبات" }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
                Aliases = new() { "نهاية فترة تسجيل المقررات للطالب", "نهاية تسجيل المقررات للطالب" }
            },
            new EventSpec
            {
                EventName = "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات",
                Aliases = new() { "بداية فترة تسجيل المقررات للمرشدين", "بداية تسجيل المقررات للمرشدين", "للمرشدين الاكاديميين" }
            },
            new EventSpec
            {
                EventName = "نهاية فترة التسجيل للمرشدين الأكاديميين",
                Aliases = new() { "نهاية فترة التسجيل للمرشدين", "نهاية التسجيل للمرشدين" }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي",
                Aliases = new() { "بداية تقديم طلبات سحب مقرر", "بداية سحب مقرر", "سحب مقرر" }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي",
                Aliases = new() { "نهاية فترة تقديم طلب سحب مقرر", "نهاية تقديم طلب سحب مقرر", "نهاية فترة سحب مقرر" }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات التأجيل",
                Aliases = new() { "بداية تقديم طلبات التأجيل", "بداية التأجيل", "تقديم طلبات التأجيل" }
            },
            new EventSpec
            {
                EventName = "نهاية تقديم طلبات التأجيل",
                Aliases = new() { "نهاية تقديم طلبات التأجيل", "نهاية التأجيل" }
            },
            new EventSpec
            {
                EventName = "بداية تقديم طلبات الاعتذار",
                Aliases = new() { "بداية تقديم طلبات الاعتذار", "بداية الاعتذار", "تقديم طلبات الاعتذار" }
            },
            new EventSpec
            {
                EventName = "نهاية فترة تقديم طلبات الاعتذار",
                Aliases = new() { "نهاية فترة تقديم طلبات الاعتذار", "نهاية تقديم طلبات الاعتذار", "نهاية الاعتذار" }
            }
        };

        public async Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId)
        {
            var nativeText = await _pdfText.ExtractTextAsync(pdfPath);
            var rawText = IsWeakText(nativeText)
                ? await _ocr.ExtractTextByOcrAsync(pdfPath)
                : nativeText;

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("Could not extract any text from PDF.");

            var cleaned = NormalizeText(rawText);

            // نبني المقاطع حسب التاريخ الميلادي
            var segments = BuildGregorianSegments(cleaned);

            if (segments.Count == 0)
                throw new InvalidOperationException("No Gregorian dates found in extracted text.");

            var used = new HashSet<int>();
            var results = new List<AcademicCalendarEvent>();

            foreach (var spec in TargetEvents)
            {
                int bestIdx = FindBestSegmentIndex(segments, spec, used);
                var seg = bestIdx >= 0 ? segments[bestIdx] : null;

                if (seg == null)
                    throw new InvalidOperationException($"Could not find Gregorian date for event: {spec.EventName}");

                results.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = spec.EventName,
                    DayAr = null,
                    HijriDate = "—",
                    GregorianDate = seg.Gregorian
                });
            }

            ValidateResults(results);
            return results;
        }

        private static List<Segment> BuildGregorianSegments(string text)
        {
            // يلتقط التاريخ الميلادي إذا كان معه م
            // أمثلة:
            // 18/08/2025م
            // 18/08/2025 م
            var pat = @"(?<!\d)(\d{1,2})\s*/\s*(\d{1,2})\s*/\s*(\d{4})\s*م\b";

            var matches = Regex.Matches(text, pat, RegexOptions.Singleline);
            var segments = new List<Segment>();

            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];

                string dd = m.Groups[1].Value.PadLeft(2, '0');
                string mm = m.Groups[2].Value.PadLeft(2, '0');
                string yyyy = m.Groups[3].Value;

                var gregText = $"{dd}/{mm}/{yyyy}";

                if (!DateTime.TryParseExact(
                    gregText,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var gregDate))
                {
                    continue;
                }

                int start = m.Index + m.Length;
                int end = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;

                var chunk = text.Substring(start, end - start);
                chunk = CleanupChunk(chunk);

                segments.Add(new Segment
                {
                    Gregorian = gregDate,
                    Text = chunk
                });
            }

            return segments;
        }

        private static int FindBestSegmentIndex(List<Segment> segments, EventSpec spec, HashSet<int> used)
        {
            int bestIdx = -1;
            int bestScore = -1;

            for (int i = 0; i < segments.Count; i++)
            {
                var norm = NormalizeForSearch(segments[i].Text);
                int score = Score(norm, spec);

                if (used.Contains(i))
                    score -= 30;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }

            if (bestIdx != -1)
                used.Add(bestIdx);

            return bestIdx;
        }

        private static int Score(string normalizedText, EventSpec spec)
        {
            int best = 0;

            foreach (var alias in spec.Aliases)
            {
                var a = NormalizeForSearch(alias);

                if (normalizedText.Contains(a))
                    best = Math.Max(best, 300 + a.Length);

                var words = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
                int matched = words.Count(w => normalizedText.Contains(w));
                best = Math.Max(best, matched * 25);
            }

            return best;
        }

        private static string NormalizeText(string raw)
        {
            var text = OcrCleaner.Clean(raw);

            // توحيد الأرقام
            text = ConvertArabicDigitsToEnglish(text);

            // توحيد / بين التواريخ
            text = Regex.Replace(text, @"\s*/\s*", "/");

            // توحيد ODUS PLUS
            text = Regex.Replace(
                text,
                @"PLUS\s*O|O\s*PLUS|ODUS\s*PLUS|PLUS\s*ODUS|ODUSPLUS|ODUS",
                "ODUS PLUS",
                RegexOptions.IgnoreCase);

            // توحيد شكل السنة مع م
            text = Regex.Replace(text, @"(\d{4})\s*م", "$1م");

            // تحويل الأسطر لمسافات
            text = text.Replace("\r\n", "\n");
            text = Regex.Replace(text, @"\n{2,}", "\n");
            text = text.Replace("\n", " ");

            text = Regex.Replace(text, @"\s{2,}", " ").Trim();
            return text;
        }

        private static string CleanupChunk(string chunk)
        {
            if (string.IsNullOrWhiteSpace(chunk))
                return "";

            chunk = chunk.Trim();

            if (chunk.Length > 800)
                chunk = chunk[..800];

            return Regex.Replace(chunk, @"\s{2,}", " ").Trim();
        }

        private static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = ConvertArabicDigitsToEnglish(text);

            text = text.Replace("أ", "ا")
                       .Replace("إ", "ا")
                       .Replace("آ", "ا")
                       .Replace("ى", "ي")
                       .Replace("ة", "ه")
                       .Replace("ؤ", "و")
                       .Replace("ئ", "ي")
                       .Replace("ـ", "");

            text = Regex.Replace(text, @"[\u064B-\u065F]", "");
            text = Regex.Replace(text, @"[^\w\s/]", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text.ToLowerInvariant();
        }

        private static string ConvertArabicDigitsToEnglish(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var map = new Dictionary<char, char>
            {
                ['٠'] = '0',
                ['١'] = '1',
                ['٢'] = '2',
                ['٣'] = '3',
                ['٤'] = '4',
                ['٥'] = '5',
                ['٦'] = '6',
                ['٧'] = '7',
                ['٨'] = '8',
                ['٩'] = '9',
                ['۰'] = '0',
                ['۱'] = '1',
                ['۲'] = '2',
                ['۳'] = '3',
                ['۴'] = '4',
                ['۵'] = '5',
                ['۶'] = '6',
                ['۷'] = '7',
                ['۸'] = '8',
                ['۹'] = '9'
            };

            var chars = input.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (map.TryGetValue(chars[i], out var replacement))
                    chars[i] = replacement;
            }

            return new string(chars);
        }

        private static bool IsWeakText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            if (text.Length < 500)
                return true;

            var bad = text.Count(c => c == '�');
            return bad > 10;
        }

        private static void ValidateResults(List<AcademicCalendarEvent> events)
        {
            if (events == null || events.Count != 10)
                throw new InvalidOperationException($"Expected 10 events, got {events?.Count ?? 0}.");

            var duplicatedNames = events
                .GroupBy(e => e.EventName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedNames.Any())
                throw new InvalidOperationException("Duplicate event names found in extracted results.");
        }
    }
}