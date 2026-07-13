using System.Threading.Tasks;

namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IPdfOcrService
    {
    
        Task<string> ExtractTextByOcrAsync(string pdfPath);
        Task<string> ExtractPageTextByOcrAsync(string pdfPath, int pageNumber);
    }
}