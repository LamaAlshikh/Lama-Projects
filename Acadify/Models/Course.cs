using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

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

    [StringLength(200)]
    public string? GraduationRequirement { get; set; }

    [ForeignKey("CourseId")]
    [InverseProperty("Courses")]
    public virtual ICollection<StudyPlan> Plans { get; set; } = new List<StudyPlan>();

    [ForeignKey("CourseId")]
    [InverseProperty("Courses")]
    public virtual ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();

}
