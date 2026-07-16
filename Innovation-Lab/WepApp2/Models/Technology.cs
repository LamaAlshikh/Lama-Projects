using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WepApp2.Models;

public partial class Technology
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TechnologyID { get; set; }

    [Required(ErrorMessage = "اسم التقنية مطلوب")]

    public string TechnologyName { get; set; } = null!;

    [Required(ErrorMessage = "وصف التقنية مطلوب")]
    public string TechnologyDescription { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}
