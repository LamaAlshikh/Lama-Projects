using System;

namespace Acadify.Models
{
    public class NotificationViewModel
    {
        public int NotificationID { get; set; }

        public string NotificationContent { get; set; } = string.Empty;

        public DateTime NotificationDate { get; set; }

        public string NotificationType { get; set; } = "System";

        public string Title { get; set; } = string.Empty;

        public string SenderName { get; set; } = "System";

        public bool IsRead { get; set; } = false;

        public string TargetUrl { get; set; } = "#";

        public string TimeAgo { get; set; } = string.Empty;

        public string SourceType { get; set; } = "General";

        public string Initials { get; set; } = "SY";
    }
}