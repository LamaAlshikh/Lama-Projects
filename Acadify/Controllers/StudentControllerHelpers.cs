using System.Text;
using UglyToad.PdfPig;

namespace Acadify.Controllers
{
    internal static class StudentControllerHelpers
    {

        // =========================
        // Helper: Read PDF Text (ONE copy فقط)
        // =========================
        private static string ReadPdfText(string fullPath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(fullPath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
        }
    }
}