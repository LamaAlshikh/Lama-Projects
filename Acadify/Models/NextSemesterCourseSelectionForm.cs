using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

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
    [StringLength(120)]
    public string? TrackChoice { get; set; }

    [Column("gpaChange")]
    [StringLength(50)]
    public string? GpaChange { get; set; }

    [Column("prerequisiteViolation")]
    [StringLength(200)]
    public string? PrerequisiteViolation { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("NextSemesterCourseSelectionForm")]
    public virtual Form Form { get; set; } = null!;
}
