using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("AcademicCalendarEvent")]
public partial class AcademicCalendarEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("gregorianDate", TypeName = "date")]
    public DateTime GregorianDate { get; set; }

    [Column("hijriDate")]
    [StringLength(20)]
    public string? HijriDate { get; set; }

    [Column("dayAr")]
    [StringLength(20)]
    public string? DayAr { get; set; }

    [Required]
    [Column("eventName")]
    [StringLength(500)]
    public string EventName { get; set; } = null!;

    [Column("calendarID")]
    public int CalendarId { get; set; }

    [ForeignKey(nameof(CalendarId))]
    [InverseProperty(nameof(AcademicCalendar.Events))]
    public virtual AcademicCalendar AcademicCalendar { get; set; } = null!;
}