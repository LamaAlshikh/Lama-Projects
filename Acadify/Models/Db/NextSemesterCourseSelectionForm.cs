using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("NextSemesterCourseSelectionForm")]
public partial class NextSemesterCourseSelectionForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("recommendedCourses")]
    public string? RecommendedCourses { get; set; }

    [Column("recommendedHours")]
    public int? RecommendedHours { get; set; }

    [Column("trackChoice")]
    [StringLength(100)]
    public string? TrackChoice { get; set; }

    [Column("gpaChange")]
    [StringLength(100)]
    public string? GpaChange { get; set; }

    [Column("prerequisiteViolation")]
    public string? PrerequisiteViolation { get; set; }

    [ForeignKey(nameof(FormId))]
    [InverseProperty(nameof(Form.NextSemesterCourseSelectionForm))]
    public virtual Form Form { get; set; } = null!;
}