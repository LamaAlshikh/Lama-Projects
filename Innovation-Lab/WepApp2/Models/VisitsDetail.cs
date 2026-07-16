using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//------00

namespace WepApp2.Models;

public partial class VisitsDetail
{
    public int VisitDetailsID { get; set; }

    public string VisitType { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<LabVisit> LabVisits { get; set; } = new List<LabVisit>();

    internal IEnumerable<object> Select(Func<object, object> value)
    {
        throw new NotImplementedException();
    }
}
