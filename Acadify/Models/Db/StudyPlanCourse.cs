using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db
{
    [Table("StudyPlanCourse")]
    public class StudyPlanCourse
    {
        [Column("planID")]
        public int PlanId { get; set; }

        [Column("courseID")]
        [StringLength(20)]
        public string CourseId { get; set; } = string.Empty;

        [Column("semesterNo")]
        public int SemesterNo { get; set; }

        [Column("displayOrder")]
        public int DisplayOrder { get; set; }
    }
}