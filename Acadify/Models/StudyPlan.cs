using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("StudyPlan")]
public partial class StudyPlan
{
    [Key]
    [Column("planID")]
    public int PlanId { get; set; }

    [Column("major")]
    [StringLength(120)]
    public string Major { get; set; } = null!;

    [Column("totalHours")]
    public int TotalHours { get; set; }

    [Column("pdfFile")]
    [StringLength(255)]
    public string? PdfFile { get; set; }

    [ForeignKey("PlanId")]
    [InverseProperty("Plans")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
