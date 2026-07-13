using System.Collections.Generic;

namespace Acadify.Models
{
    public class Form4ViewModel
    {
        public string StudentName { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string AcademicYear { get; set; } = "2024";

        public int EarnedHours { get; set; } = 0;
        public int RegisteredHours { get; set; } = 0;

        public int UniversityReqHours { get; set; } = 0;
        public int PrepYearReqHours { get; set; } = 0;
        public int FreeCoursesHours { get; set; } = 0;
        public int CollegeMandatoryHours { get; set; } = 0;
        public int DeptMandatoryHours { get; set; } = 0;
        public int DeptElectiveHours { get; set; } = 0;

        public int TotalHours { get; set; } = 0;
        public string GraduationTermText { get; set; } = "";

        public string Note1 { get; set; } = "";
        public string Note2 { get; set; } = "";
        public string Note3 { get; set; } = "";
        public string Note4 { get; set; } = "";

        public string AdvisorNameLabel { get; set; } = "المرشدة الأكاديمية للطالبة";
        public string AdvisorName { get; set; } = "";
        public string AdvisorSignature { get; set; } = "";
        public string AdvisorNotes { get; set; } = "";

        public string Status { get; set; } = "Draft";

        public List<Form4CourseDecisionItemVM> PendingCourses { get; set; } = new();
        public List<PlanCourseOptionVM> PlanCourseOptions { get; set; } = new();
    }
}