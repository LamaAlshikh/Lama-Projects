using System.Text.RegularExpressions;

namespace Acadify.Services.AcademicCalendar
{
    public static class OcrCleaner
    {
        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = raw;

            text = Regex.Replace(
                text,
                @"ODUS\s*PLUS|PLUS\s*ODUS|ODUSPLUS|ODUS\s+PLUS|ODUS",
                "ODUS PLUS",
                RegexOptions.IgnoreCase);

            text = Regex.Replace(text, @"\([A-Za-z]\d+\)", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"[■◆●•]+", " ");
            text = text.Replace("\r\n", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = Regex.Replace(text, @"[ \t]{2,}", " ");

            return text.Trim();
        }
    }
}