using System.Collections.Generic;

namespace Acadify.Models
{
    public class StudentChatViewModel
    {
        public string AdvisorName { get; set; } = "";
        public string StudentName { get; set; } = "";

        // لمتابعة حالة التسجيل الصوتي أو أرشفة الجلسة
        public bool IsRecordingStarted { get; set; } = false;

        public List<ChatMessageVM> Messages { get; set; } = new();
    }

    public class ChatMessageVM
    {
        public string SenderName { get; set; } = "";
        public string Text { get; set; } = "";

        // true تظهر جهة اليمين (الطالب)، false تظهر جهة اليسار (المرشد)
        public bool IsFromStudent { get; set; }

        public string TimeText { get; set; } = "";

        // الخاصية التي أجمعت عليها النسخ (لتمييز إذا كانت الرسالة مسجلة نظاماً)
        public bool IsRecorded { get; set; } = false;
    }
}