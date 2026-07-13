using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("MatchingStatus")]
[Index("StudentId", Name = "UQ__Matching__4D11D65D005ED580", IsUnique = true)]
public partial class MatchingStatus
{
    [Key]
    [Column("statusID")]
    public int StatusId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("status")]
    [StringLength(80)]
    public string Status { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("MatchingStatus")]
    public virtual Student Student { get; set; } = null!;
}
