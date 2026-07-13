using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("MeetingForm")]
public partial class MeetingForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("meetingID")]
    public int? MeetingId { get; set; }

    [Column("meetingStart")]
    public DateTime? MeetingStart { get; set; }

    [Column("meetingEnd")]
    public DateTime? MeetingEnd { get; set; }

    [Column("meetingPurpose")]
    [StringLength(100)]
    public string? MeetingPurpose { get; set; }

    [Column("meetingNotes")]
    public string? MeetingNotes { get; set; }

    [Column("referralReason")]
    public string? ReferralReason { get; set; }

    [Column("referredTo")]
    [StringLength(120)]
    public string? ReferredTo { get; set; }

    [Column("studentActions")]
    public string? StudentActions { get; set; }

    [Column("advisorActions")]
    public string? AdvisorActions { get; set; }

    [ForeignKey(nameof(FormId))]
    [InverseProperty(nameof(Form.MeetingForm))]
    public virtual Form Form { get; set; } = null!;
}