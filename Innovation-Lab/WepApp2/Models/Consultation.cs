using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//------------00

namespace WepApp2.Models;

public partial class Consultation
{

    public int ConsultationID { get; set; }

    public string ConsultationDescription { get; set; } = null!;

    public DateOnly ConsultationDate { get; set; }

    public TimeOnly AvailableTimes { get; set; }

    public string? PreferredContactMethod { get; set; }

    public int? ServiceId { get; set; }

    public int? RequestId { get; set; }

    public int ConsultationMajorId { get; set; }

    public virtual ConsultationMajor ConsultationMajor { get; set; } = null!;

    public virtual Request? Request { get; set; }

    public virtual Service? Service { get; set; }
}
