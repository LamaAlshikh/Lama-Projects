using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db;

[Table("Notification")]
[Index(nameof(StudentId), Name = "IX_Notif_StudentID")]
[Index(nameof(AdvisorId), Name = "IX_Notif_AdvisorID")]
[Index(nameof(AdminId), Name = "IX_Notif_AdminID")]
public partial class Notification
{
    [Key]
    [Column("notificationID")]
    public int NotificationId { get; set; }

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("type")]
    [StringLength(100)]
    public string? Type { get; set; }

    [Column("senderRole")]
    [StringLength(50)]
    public string? SenderRole { get; set; }

    [Column("sourceType")]
    [StringLength(50)]
    public string? SourceType { get; set; }

    [Column("advisorID")]
    public int? AdvisorId { get; set; }

    [Column("studentID")]
    public int? StudentId { get; set; }

    [Column("adminID")]
    public int? AdminId { get; set; }

    [Column("isRead")]
    public bool IsRead { get; set; } = false;

    [ForeignKey(nameof(AdvisorId))]
    [InverseProperty(nameof(Advisor.Notifications))]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey(nameof(StudentId))]
    [InverseProperty(nameof(Student.Notifications))]
    public virtual Student? Student { get; set; }

    [ForeignKey(nameof(AdminId))]
    [InverseProperty(nameof(Admin.Notifications))]
    public virtual Admin? Admin { get; set; }
}