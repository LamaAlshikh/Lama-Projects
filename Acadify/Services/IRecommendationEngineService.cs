using Acadify.Models;

namespace Acadify.Services
{
    public interface IRecommendationEngineService
    {
        Task<List<RecommendedCourseVm>> GenerateRecommendationsAsync(
            int planId,
            List<TranscriptCourseItem> transcriptCourses);
    }
}