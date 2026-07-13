namespace Acadify.Models
{
    public class CommunityMessageVM
    {
        public string SenderName { get; set; } = string.Empty;
        public string SenderInitials { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public bool IsAdvisorMessage { get; set; }
        public bool IsCurrentUserMessage { get; set; }
        public string BubbleColorClass { get; set; } = string.Empty;
        public string ImagePath { get; set; } = "~/images/user.png";
    }
}
