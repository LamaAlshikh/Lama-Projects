using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp2.Models
{
    public class ServiceDevice
    {
        [Key]
        public int ServiceDeviceID { get; set; }

        [Required]
        public int ServiceID { get; set; }

        [Required]
        public int DeviceID { get; set; }

        // علاقات الربط مع الجداول الأخرى
        [ForeignKey("ServiceID")]
        public Service Service { get; set; }

        [ForeignKey("DeviceID")]
        public Device Device { get; set; }
    }
}
