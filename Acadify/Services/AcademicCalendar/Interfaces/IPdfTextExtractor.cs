namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IPdfTextExtractor
    {
        Task<string> ExtractTextAsync(string pdfPath);
    }
}