using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("GraduationStatus")]
public partial class GraduationStatus
{
    [Key]
    [Column("statusID")]
    public int StatusId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("status")]
    [StringLength(80)]
    public string Status { get; set; } = null!;

    [Column("remainingHours")]
    public int RemainingHours { get; set; }

    [ForeignKey(nameof(StudentId))]
    [InverseProperty(nameof(Student.GraduationStatus))]
    public virtual Student Student { get; set; } = null!;
}