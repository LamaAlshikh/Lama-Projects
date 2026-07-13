using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("MeetingForm")]
public partial class MeetingForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("meetingStart")]
    public DateTime? MeetingStart { get; set; }

    [Column("meetingEnd")]
    public DateTime? MeetingEnd { get; set; }

    [Column("meetingPurpose")]
    [StringLength(200)]
    public string? MeetingPurpose { get; set; }

    [Column("meetingNotes")]
    public string? MeetingNotes { get; set; }

    [Column("referralReason")]
    [StringLength(200)]
    public string? ReferralReason { get; set; }

    [Column("referredTo")]
    [StringLength(200)]
    public string? ReferredTo { get; set; }

    [Column("studentActions")]
    public string? StudentActions { get; set; }

    [Column("advisorActions")]
    public string? AdvisorActions { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("MeetingForm")]
    public virtual Form Form { get; set; } = null!;
}
