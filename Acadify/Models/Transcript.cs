using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Transcript")]
[Index("StudentId", Name = "UQ__Transcri__4D11D65DABAA087B", IsUnique = true)]
public partial class Transcript
{
    [Key]
    [Column("transcriptID")]
    public int TranscriptId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("pdfFile")]
    [StringLength(300)]
    public string? PdfFile { get; set; }

    [Column("GPA", TypeName = "decimal(4, 2)")]
    public decimal? Gpa { get; set; }

    [Column("semesterGPA", TypeName = "decimal(4, 2)")]
    public decimal? SemesterGpa { get; set; }

    [Column("extractedInfo")]
    public string? ExtractedInfo { get; set; }

    [Column("extractedCourses")]
    public string? ExtractedCourses { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Transcript")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("TranscriptId")]
    [InverseProperty("Transcripts")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
