using System;
using System.ComponentModel.DataAnnotations;

namespace Acadify.Models
{
    public class Form1ViewModel
    {
        [Required] public string FullName { get; set; } = "";
        [Required] public string StudentId { get; set; } = "";

        public string Age { get; set; } = "";
        public string YearOfEnrollment { get; set; } = "";
        public string YearOfStudy { get; set; } = "";
        public string GpaCurrent { get; set; } = "";
        public string TermSemester { get; set; } = "";
        public string MobilePhone { get; set; } = "";
        public string Email { get; set; } = "";

        public string AdvisorName { get; set; } = "";
        public string MedicalNeedsOptional { get; set; } = "";

        public DateTime? ApprovalDate { get; set; }
        public DateTime? AdvisingCommencementDate { get; set; }

        public string Status { get; set; } = "Draft";
    }
}