using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class PdfOcrService : IPdfOcrService
    {
        private readonly OpenAiVisionClient _vision;

        public PdfOcrService(OpenAiVisionClient vision)
        {
            _vision = vision;
        }

        /// <summary>
        /// استخراج النص من كافة صفحات ملف الـ PDF باستخدام OpenAI Vision
        /// </summary>
        public async Task<string> ExtractTextByOcrAsync(string pdfPath)
        {
            // تحويل صفحات PDF إلى صور PNG (تأكدي من وجود كلاس مساعد PdfToImages)
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < images.Count; i++)
            {
                var pageText = await ExtractTextFromSingleImageAsync(images[i]);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine($"[Page {i + 1}]");
                    sb.AppendLine(pageText);
                    sb.AppendLine("-----PAGE_BREAK-----");
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// استخراج النص من صفحة محددة فقط
        /// </summary>
        public async Task<string> ExtractPageTextByOcrAsync(string pdfPath, int pageNumber)
        {
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0 || pageNumber < 1 || pageNumber > images.Count)
                return string.Empty;

            var singlePageImage = images[pageNumber - 1];
            return await ExtractTextFromSingleImageAsync(singlePageImage);
        }

        /// <summary>
        /// الميثود المسؤولة عن إرسال الصورة لـ OpenAI ومعالجة النص
        /// </summary>
        private async Task<string> ExtractTextFromSingleImageAsync(byte[] pngImage)
        {
            var prompt = """
            اقرأ النص العربي الموجود في الصورة كما هو.
            هذه صفحة من تقويم أكاديمي.
            استخرج النص فقط.
            لا تشرح، لا تلخص، ولا تعيده بصيغة JSON.
            أعد النص الخام فقط مع الحفاظ قدر الإمكان على الكلمات والتواريخ.
            """;

            var text = await _vision.GetJsonFromImagesAsync(
                prompt,
                new List<byte[]> { pngImage });

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("OCR returned empty text from image.");

            return text.Trim();
        }
    }
}