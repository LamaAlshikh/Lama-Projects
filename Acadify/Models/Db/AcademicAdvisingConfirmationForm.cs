using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("AcademicAdvisingConfirmationForm")]
public partial class AcademicAdvisingConfirmationForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("studentName")]
    [StringLength(120)]
    public string? StudentName { get; set; }

    [Column("studentLevel")]
    [StringLength(50)]
    public string? StudentLevel { get; set; }

    [Column("currentGPA", TypeName = "decimal(4, 2)")]
    public decimal? CurrentGpa { get; set; }

    [Column("coursesCount")]
    public int? CoursesCount { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("AcademicAdvisingConfirmationForm")]
    public virtual Form Form { get; set; } = null!;
}