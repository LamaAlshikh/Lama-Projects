using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Acadify.Services.AcademicCalendar
{
    public static class PdfToImages
    {
        public static async Task<List<byte[]>> RenderAllPagesAsPngAsync(string pdfPath)
        {
            var images = new List<byte[]>();

            var dims = new PageDimensions(1800, 2400);

            using var docReader = DocLib.Instance.GetDocReader(pdfPath, dims);
            var pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);

                var rawBytes = pageReader.GetImage();
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException($"Invalid rendered page size: {width}x{height}");

                using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);

                await using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms);
                images.Add(ms.ToArray());
            }

            return images;
        }
    }
}