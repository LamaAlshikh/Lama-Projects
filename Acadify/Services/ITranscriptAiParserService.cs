using Acadify.Models;
using Microsoft.AspNetCore.Http;

namespace Acadify.Services
{
    public interface ITranscriptAiParserService
    {
        Task<List<TranscriptCourseItem>> ParseTranscriptAsync(IFormFile file);
    }
}