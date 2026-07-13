using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("StudyPlanMatchingForm")]
public partial class StudyPlanMatchingForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("graduationStatus")]
    [StringLength(80)]
    public string? GraduationStatus { get; set; }

    [Column("remainingHours")]
    public int? RemainingHours { get; set; }

    [Column("requiredHours")]
    public int? RequiredHours { get; set; }

    [Column("earnedHours")]
    public int? EarnedHours { get; set; }

    [Column("registeredHours")]
    public int? RegisteredHours { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("StudyPlanMatchingForm")]
    public virtual Form Form { get; set; } = null!;
}
