using System.Collections.Generic;
using System.Linq;

namespace Acadify.Models
{
    public class CourseRecommendationViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public decimal? Gpa { get; set; }
        public decimal? SemesterGpa { get; set; }

        public List<CourseCardVM> Cards { get; set; } = new();
        public List<SelectedCourseVM> Selected { get; set; } = new();

        public string FreeElectiveCourse1 { get; set; } = "";
        public string FreeElectiveCourse2 { get; set; } = "";

        public string FreeElectiveCourse3 { get; set; } = "";


        public int TotalHours => Selected.Sum(x => x.Hours);
    }

    public class CourseCardVM
    {
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int Hours { get; set; }
        public string? Prerequisite { get; set; }

        public bool IsCompleted { get; set; }
        public bool CanTake { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }

        public string Status { get; set; } = "";

        
    }

    public class SelectedCourseVM
    {
        public string CourseId { get; set; } = "";
        public int Hours { get; set; }
    }
}