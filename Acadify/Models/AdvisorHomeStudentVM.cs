namespace Acadify.Models
{
    public class AdvisorHomeStudentVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int CohortYear { get; set; }
        public string AcademicStatus { get; set; } = string.Empty;   // near graduation / has remaining courses
        public string MatchStatus { get; set; } = string.Empty;      // matched / not matched
        public string ImagePath { get; set; } = "~/images/user.png";
    }
}
