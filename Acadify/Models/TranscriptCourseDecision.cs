namespace Acadify.Models.Db
{
    public partial class TranscriptCourseDecision
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public string TranscriptCourseId { get; set; } = null!;

        public string DecisionType { get; set; } = "FreeElective";

        public string? EquivalentCourseId { get; set; }

        public bool IsApprovedByAdvisor { get; set; } = false;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Student Student { get; set; } = null!;

        public virtual Course TranscriptCourse { get; set; } = null!;

        public virtual Course? EquivalentCourse { get; set; }
    }
}