using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Meeting")]
public partial class Meeting
{
    [Key]
    [Column("meetingID")]
    public int MeetingId { get; set; }

    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("advisorID")]
    public int AdvisorId { get; set; }

    [Column("chatRecord")]
    public string? ChatRecord { get; set; }

    [Column("startTime")]
    public DateTime? StartTime { get; set; }

    [Column("endTime")]
    public DateTime? EndTime { get; set; }

    [ForeignKey("AdvisorId")]
    [InverseProperty("Meetings")]
    public virtual Advisor Advisor { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("Meetings")]
    public virtual Student Student { get; set; } = null!;
}
