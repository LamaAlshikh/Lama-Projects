using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class MatchingStatus
{
    public int StatusId { get; set; }

    public int StudentId { get; set; }

    public string Status { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}