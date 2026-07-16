using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//---------00
namespace WepApp2.Models;

public partial class ConsultationMajor
{
    public int ConsultationMajorID { get; set; }

    public string Major { get; set; } = null!;

    public string ConsultationDescription { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
