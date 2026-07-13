using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Acadify.Models.Db;

[Table("CourseChoiceMonitoringForm")]
public partial class CourseChoiceMonitoringForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("semester")]
    [StringLength(100)]
    public string? Semester { get; set; }

    [Column("comingSemester")]
    [StringLength(100)]
    public string? ComingSemester { get; set; }

    [Column("runningCreditHours")]
    public int? RunningCreditHours { get; set; }

    [Column("advisedCreditHours")]
    public int? AdvisedCreditHours { get; set; }

    [Column("level")]
    [StringLength(100)]
    public string? Level { get; set; }

    [Column("dropSubjects")]
    public string? DropSubjects { get; set; }

    [Column("isSubjects")]
    public string? ICSubjects { get; set; }

    [Column("ipSubjects")]
    public string? IpSubjects { get; set; }

    [Column("selectedCoursesJson")]
    public string? SelectedCoursesJson { get; set; }

    [ForeignKey("FormId")]
    public virtual Form Form { get; set; } = null!;
}