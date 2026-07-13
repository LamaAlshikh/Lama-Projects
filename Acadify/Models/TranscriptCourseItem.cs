namespace Acadify.Models
{
    public class TranscriptCourseItem
    {
        public string CourseId { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
    }
}