using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;




namespace WepApp2.Models;

public partial class BookingDevice
{

    [Key]
    public int BookingDeviceID { get; set; }

    public string ProjectName { get; set; } = null!;

    public string ProjectDescription { get; set; } = null!;

    public string Faculty { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string? FilePath { get; set; }

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int? ServiceId { get; set; }

    public int? DeviceId { get; set; }

    public int? RequestId { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Request? Request { get; set; }

    public virtual Service? Service { get; set; }
}
