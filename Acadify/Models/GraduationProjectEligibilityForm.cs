using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("GraduationProjectEligibilityForm")]
public partial class GraduationProjectEligibilityForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("eligibility")]
    [StringLength(50)]
    public string? Eligibility { get; set; }

    [Column("requiredCoursesStatus")]
    [StringLength(200)]
    public string? RequiredCoursesStatus { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("GraduationProjectEligibilityForm")]
    public virtual Form Form { get; set; } = null!;

    // UI-only properties for the old Advisor/Form5.cshtml view.
    // These are not database columns.
    [NotMapped]
    public string? StudentName { get; set; }

    [NotMapped]
    public string? StudentId { get; set; }

    [NotMapped]
    public bool CPIS351 { get; set; }

    [NotMapped]
    public bool CPIS358 { get; set; }

    [NotMapped]
    public bool CPIS323 { get; set; }

    [NotMapped]
    public bool CPIS360 { get; set; }

    [NotMapped]
    public bool CPIS375 { get; set; }

    [NotMapped]
    public bool CPIS342 { get; set; }

    [NotMapped]
    public bool IsEligible { get; set; }

    [NotMapped]
    public string? AdvisorComment { get; set; }

    [NotMapped]
    public string? FormStatus { get; set; }

    [NotMapped]
    public DateTime CreatedDate { get; set; }
}
