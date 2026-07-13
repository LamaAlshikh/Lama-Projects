using System.Collections.Generic;


namespace Acadify.Models
{
    public class Form2ViewModel
    {
        // Student section (running semester)
        public string StudentName { get; set; } = "";
        public int StudentId { get; set; }
        public string Semester { get; set; } = "";     // Fall/Spring/Summer
        public int RunningCreditHours { get; set; }

        public string DropSubjects { get; set; } = "";
        public string ICSubjects { get; set; } = "";
        public string IPSubjects { get; set; } = "";

        // Course choices (coming semester)
        public int AdvisedCreditHours { get; set; }
        public string ComingSemester { get; set; } = "";
        public string Level { get; set; } = "";
        public string TrackChoice { get; set; } = "";

        public List<SelectedCourseVM> SelectedCourses { get; set; } = new();

        // Advisor follow-up (week 2) – نخليها فاضية الآن
        public string GpaChanged { get; set; } = ""; // No / increased / decreased
        public string GpaChangeReason { get; set; } = "";
        public string ActionsTaken { get; set; } = "";
    }
}