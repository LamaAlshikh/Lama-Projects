using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Advisor")]
public partial class Advisor
{
    [Key]
    [Column("advisorID")]
    public int AdvisorId { get; set; }

    [Column("userID")]
    public int UserId { get; set; }

    [Column("department")]
    [StringLength(120)]
    public string? Department { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Advisor")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Advisor")]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}