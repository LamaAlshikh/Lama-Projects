using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class VwMyStudent
{
    public int? CohortYear { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public string? GraduationStatus { get; set; }

    public string? MatchingStatus { get; set; }

    public int? AdvisorId { get; set; }

}
