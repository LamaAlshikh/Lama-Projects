using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//-----00
namespace WepApp2.Models;

public partial class Request
{
    public int RequestID { get; set; }

    public string RequestType { get; set; } = null!;

    public string SupervisorStatus { get; set; } = null!;

    public DateTime RequestDate { get; set; }

    public int? SupervisorAssigned { get; set; }

    public string? AdminStatus { get; set; }

    public string? Notes { get; set; }

    public int UserId { get; set; }

    public int ServiceId { get; set; }

    public int? DeviceId { get; set; }

    public int? CourseID { get; set; }

    public virtual ICollection<BookingDevice> BookingDevices { get; set; } = new List<BookingDevice>();

    public virtual ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();

    public virtual Course? Course { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<DeviceLoan> DeviceLoans { get; set; } = new List<DeviceLoan>();

    public virtual ICollection<LabVisit> LabVisits { get; set; } = new List<LabVisit>();

    public virtual Service Service { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
