using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db
{
    [Table("Meeting")]
    public partial class Meeting
    {
        public int MeetingId { get; set; }

        public int StudentId { get; set; }

        public int AdvisorId { get; set; }

        // تم دمج حقول الدردشة: السجل الكامل والملخص الآلي
        public string? ChatRecord { get; set; }
        public string? ChatSummary { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // --- حقول إدارة التسجيل (من نسخة رهف ولينا) ---
        public bool IsRecordingStarted { get; set; }
        public string? LastRecordingAction { get; set; }
        public DateTime? RecordingStartedAt { get; set; }
        public DateTime? RecordingStoppedAt { get; set; }

        // --- العلاقات (Navigation Properties) ---
        public virtual Advisor Advisor { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}