namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IChatClient
    {
        Task<string> GetJsonAsync(string prompt);
    }
}