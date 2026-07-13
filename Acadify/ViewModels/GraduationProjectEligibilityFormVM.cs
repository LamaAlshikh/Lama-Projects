using System;
using Db = Acadify.Models.Db;

namespace Acadify.ViewModels
{
    public class GraduationProjectEligibilityFormVM
    {
        public int FormId { get; set; }

        public Db.Form? Form { get; set; }

        public string StudentName { get; set; } = "-";

        public string StudentId { get; set; } = "-";

        public bool CPIS351 { get; set; }

        public bool CPIS358 { get; set; }

        public bool CPIS323 { get; set; }

        public bool CPIS380 { get; set; }

        public bool CPIS357 { get; set; }

        public bool CPIS342 { get; set; }

        public bool IsEligible { get; set; }

        public string? Eligibility { get; set; }

        public string? RequiredCoursesStatus { get; set; }

        public string? AdvisorComment { get; set; }

        public string? FormStatus { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsHistoryView { get; set; }

        public bool IsEditMode { get; set; }
    }
}