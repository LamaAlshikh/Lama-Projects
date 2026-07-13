using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

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

    [ForeignKey(nameof(FormId))]
    [InverseProperty(nameof(Form.GraduationProjectEligibilityForm))]
    public virtual Form Form { get; set; } = null!;

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
    public bool CPIS380 { get; set; }

    [NotMapped]
    public bool CPIS357 { get; set; }

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