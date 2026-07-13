// Models/Form.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Index("AdvisorId", Name = "IX_Forms_AdvisorID")]
[Index("StudentId", Name = "IX_Forms_StudentID")]
public partial class Form
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("advisorID")]
    public int AdvisorId { get; set; }

    [Column("formType")]
    [StringLength(80)]
    public string FormType { get; set; } = null!;

    [Column("formDate")]
    public DateTime FormDate { get; set; }

    [Column("formStatus")]
    [StringLength(60)]
    public string FormStatus { get; set; } = null!;

    [Column("advisorNotes")]
    public string? AdvisorNotes { get; set; }

    [Column("autoFilled")]
    public bool AutoFilled { get; set; }

    public bool? AdvisorConfirmation { get; set; }

    [InverseProperty("Form")]
    public virtual AcademicAdvisingConfirmationForm? AcademicAdvisingConfirmationForm { get; set; }

    [ForeignKey("AdvisorId")]
    [InverseProperty("Forms")]
    public virtual Advisor Advisor { get; set; } = null!;

    [InverseProperty("Form")]
    public virtual GraduationProjectEligibilityForm? GraduationProjectEligibilityForm { get; set; }

    [InverseProperty("Form")]
    public virtual MeetingForm? MeetingForm { get; set; }

    [InverseProperty("Form")]
    public virtual NextSemesterCourseSelectionForm? NextSemesterCourseSelectionForm { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Forms")]
    public virtual Student Student { get; set; } = null!;

    [InverseProperty("Form")]
    public virtual StudyPlanMatchingForm? StudyPlanMatchingForm { get; set; }
}