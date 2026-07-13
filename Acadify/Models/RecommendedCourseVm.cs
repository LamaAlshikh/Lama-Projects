namespace Acadify.Models
{
    public class RecommendedCourseVm
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Hours { get; set; }
        public int SemesterNo { get; set; }
        public int DisplayOrder { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
