using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("GraduationStatus")]
[Index("StudentId", Name = "UQ__Graduati__4D11D65DD7D26C0D", IsUnique = true)]
public partial class GraduationStatus
{
    [Key]
    [Column("statusID")]
    public int StatusId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("status")]
    [StringLength(80)]
    public string Status { get; set; } = null!;

    [Column("remainingHours")]
    public int RemainingHours { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("GraduationStatus")]
    public virtual Student Student { get; set; } = null!;
}
