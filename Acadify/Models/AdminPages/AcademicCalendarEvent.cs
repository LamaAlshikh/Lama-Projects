using Acadify.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AcademicCalendarEvent
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Column("gregorianDate", TypeName = "date")]
    public DateTime GregorianDate { get; set; }
    [StringLength(20)]
    public string? HijriDate { get; set; }

    [StringLength(20)]
    public string? DayAr { get; set; } // ✅ NEW

    [Required, StringLength(500)]
    public string EventName { get; set; } = null!;

    public int CalendarId { get; set; }

    [ForeignKey("CalendarId")]
    public virtual AcademicCalendar AcademicCalendar { get; set; } = null!;
}