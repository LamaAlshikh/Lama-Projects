using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("Course")]
public partial class Course
{
    [Key]
    [Column("courseID")]
    [StringLength(30)]
    public string CourseId { get; set; } = null!;

    [Column("courseName")]
    [StringLength(200)]
    public string CourseName { get; set; } = null!;

    [Column("hours")]
    public int Hours { get; set; }

    [Column("prerequisite")]
    [StringLength(200)]
    public string? Prerequisite { get; set; }

    [Column("GraduationRequirement")]
    [StringLength(200)]
    public string? GraduationRequirement { get; set; }

    [Column("RequirementCategory")]
    [StringLength(200)]
    public string? RequirementCategory { get; set; }

    public virtual ICollection<StudyPlan> Plans { get; set; } = new List<StudyPlan>();

    public virtual ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
}