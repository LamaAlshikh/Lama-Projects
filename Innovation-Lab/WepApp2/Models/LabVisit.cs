using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//-----00

namespace WepApp2.Models;

public partial class LabVisit
{
    public int LabVisitID { get; set; }

    public int NumberOfVisitors { get; set; }

    public DateTime VisitDate { get; set; }

    public TimeSpan PreferredTime { get; set; }

    public string? PreferredContactMethod { get; set; }

    public string? AdditionalNotes { get; set; }

    public int? ServiceId { get; set; }

    public int? RequestId { get; set; }

    public int? VisitDetailsId { get; set; }

    public virtual Request? Request { get; set; }

    public virtual Service? Service { get; set; }

    public virtual VisitsDetail? VisitDetails { get; set; }
}
