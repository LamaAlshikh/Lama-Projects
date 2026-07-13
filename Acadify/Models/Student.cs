using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

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

    // --- العلاقات (Navigation Properties) ---

    [ForeignKey(nameof(AdvisorId))]
    [InverseProperty(nameof(Models.Advisor.Students))]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Models.User.Student))]
    public virtual User User { get; set; } = null!;

    [InverseProperty(nameof(Models.Form.Student))]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty(nameof(Models.GraduationStatus.Student))]
    public virtual GraduationStatus? GraduationStatus { get; set; }

    [InverseProperty(nameof(Models.MatchingStatus.Student))]
    public virtual MatchingStatus? MatchingStatus { get; set; }

    [InverseProperty(nameof(Models.Meeting.Student))]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty(nameof(Models.Notification.Student))]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty(nameof(Models.Transcript.Student))]
    public virtual Transcript? Transcript { get; set; }
}