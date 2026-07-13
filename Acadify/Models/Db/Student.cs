using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db;

[Table("Student")]
[Index(nameof(AdvisorId), Name = "IX_Student_AdvisorID")]
[Index(nameof(UserId), Name = "UQ_Student_UserID", IsUnique = true)]
public partial class Student
{
    [Key]
    [Column("studentID")]
    public int StudentId { get; set; }

    [Column("userID")]
    public int UserId { get; set; }

    [StringLength(120)]
    public string Name { get; set; } = null!;

    [Column("major")]
    [StringLength(120)]
    public string? Major { get; set; }

    [Column("level")]
    [StringLength(50)]
    public string? Level { get; set; }

    [Column("completedHours")]
    public int CompletedHours { get; set; }

    [Column("cohortYear")]
    public int? CohortYear { get; set; }

    [Column("advisorID")]
    public int? AdvisorId { get; set; }

    [ForeignKey(nameof(AdvisorId))]
    [InverseProperty(nameof(Db.Advisor.Students))]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Db.User.Student))]
    public virtual User User { get; set; } = null!;

    [InverseProperty(nameof(Db.Form.Student))]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty(nameof(Db.GraduationStatus.Student))]
    public virtual GraduationStatus? GraduationStatus { get; set; }

    [InverseProperty(nameof(Db.MatchingStatus.Student))]
    public virtual MatchingStatus? MatchingStatus { get; set; }

    [InverseProperty(nameof(Db.Meeting.Student))]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty(nameof(Db.Notification.Student))]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty(nameof(Db.Transcript.Student))]
    public virtual Transcript? Transcript { get; set; }

    public virtual ICollection<TranscriptCourseDecision> TranscriptCourseDecisions { get; set; } = new List<TranscriptCourseDecision>();
}