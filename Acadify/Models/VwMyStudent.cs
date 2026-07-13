using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Keyless]
public partial class VwMyStudent
{
    [Column("cohortYear")]
    public int? CohortYear { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("studentName")]
    [StringLength(120)]
    public string StudentName { get; set; } = null!;

    [Column("graduationStatus")]
    [StringLength(80)]
    public string? GraduationStatus { get; set; }

    [Column("matchingStatus")]
    [StringLength(80)]
    public string? MatchingStatus { get; set; }

    [Column("advisorID")]
    public int? AdvisorId { get; set; }
}
