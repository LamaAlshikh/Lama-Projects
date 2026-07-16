using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp2.Models;

public partial class Device
{


    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    //---id

    public int DeviceID { get; set; }

    [Required(ErrorMessage = "اسم الجهاز مطلوب")]
    public string DeviceName { get; set; } = null!;


    [Required(ErrorMessage = "اسم الشركة المصنعة مطلوب")]
    public string BrandName { get; set; } = null!;

    public string? DeviceModel { get; set; }

    [Required(ErrorMessage = "الرقم التسلسلي مطلوب")]
    public string SerialNumber { get; set; } = null!;

    [Required(ErrorMessage = "حالة الجهاز مطلوبة")]
    public string DeviceStatus { get; set; } = null!;

    [Required(ErrorMessage = "الموقع مطلوب")]
    public string DeviceLocation { get; set; } = null!;

    public DateTime? LastMaintenance { get; set; }

    public DateTime? LastUpdate { get; set; }

    public bool IsDeleted { get; set; }


    [Required(ErrorMessage = "نوع التقنية مطلوب")]
    public int? TechnologyId { get; set; }

    public virtual ICollection<BookingDevice> BookingDevices { get; set; } = new List<BookingDevice>();

    public virtual ICollection<DeviceLoan> DeviceLoans { get; set; } = new List<DeviceLoan>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();



    [ForeignKey("TechnologyId")]
    public virtual Technology? Technology { get; set; }
}
