namespace Acadify.Models
{
    public class Form4CourseDecisionItemVM
    {
        public string TranscriptCourseId { get; set; } = "";
        public string TranscriptCourseName { get; set; } = "";
        public int Hours { get; set; }

        public string DecisionType { get; set; } = "";
        public string? EquivalentCourseId { get; set; }
    }

    public class PlanCourseOptionVM
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
    }
}