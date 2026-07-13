namespace Acadify.Models
{
    public class StudentHomeViewModel
    {
        // Student basic information
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;

        // Graduation status (حالة التخرج والتقدم)
        public int ProgressPercentage { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public int RemainingHours { get; set; }

        // Extra display (بيانات إضافية للعرض في لوحة التحكم)
        public int CompletedHours { get; set; }
        public int TotalRequiredHours { get; set; } = 140;
    }
}