using System;
using System.Collections.Generic;

namespace Acadify.Models.Db
{
    public partial class StudyPlanMatchingForm
    {
        public int FormId { get; set; }

        public string? GraduationStatus { get; set; }

        // --- توزيع الساعات الأكاديمية (التفاصيل الكاملة) ---
        public int? TotalHours { get; set; }
        public int? EarnedHours { get; set; }
        public int? RegisteredHours { get; set; }
        public int? RemainingHours { get; set; }
        public int? RequiredHours { get; set; }

        // تفاصيل الساعات حسب التصنيف (من نسخة رهف)
        public int? UniversityHours { get; set; }
        public int? PrepYearHours { get; set; }
        public int? FreeCoursesHours { get; set; }
        public int? CollegeMandatoryHours { get; set; }
        public int? DeptMandatoryHours { get; set; }
        public int? DeptElectiveHours { get; set; }

        // --- العلاقة مع النموذج الأساسي ---
        public virtual Form Form { get; set; } = null!;
    }
}