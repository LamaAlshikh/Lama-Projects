using Acadify.Services.AcademicCalendar.Interfaces;
using System.Text;
using UglyToad.PdfPig;

namespace Acadify.Services.AcademicCalendar
{
    public class PdfPigTextExtractor : IPdfTextExtractor
    {
        public Task<string> ExtractTextAsync(string pdfPath)
        {
            var sb = new StringBuilder();

            using var doc = PdfDocument.Open(pdfPath);
            foreach (var page in doc.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return Task.FromResult(sb.ToString());
        }
    }
}