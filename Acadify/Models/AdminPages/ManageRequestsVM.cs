namespace Acadify.Models.AdminPages
{
    public class ManageRequestsVM
    {
        public List<RequestRow> PendingRequests { get; set; } = new();

        public class RequestRow
        {
            // المعرفات الأساسية للعمليات البرمجية (من نسخة لما)
            public int RequestId { get; set; }
            public int StudentId { get; set; }
            public int? RequestedAdvisorId { get; set; }

            // بيانات الطالب للعرض
            public string StudentName { get; set; } = string.Empty;
            public string UniversityId { get; set; } = string.Empty;

            // بيانات المرشد المطلوب
            // تم اختيار "Not registered yet" كقيمة افتراضية في حال لم يكن المرشد مسجلاً في النظام بعد
            public string RequestedAdvisorName { get; set; } = "Not registered yet";
            public string RequestedAdvisorEmail { get; set; } = string.Empty;

            // حالة الطلب
            public string Status { get; set; } = "Pending";

            public DateTime CreatedAt { get; set; }
        }
    }
}