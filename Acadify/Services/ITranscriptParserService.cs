using Acadify.Models;

namespace Acadify.Services
{
    public interface ITranscriptParserService
    {
        Task<List<TranscriptCourseItem>> ParseTranscriptAsync(IFormFile file);
    }
}
