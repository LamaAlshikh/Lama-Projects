using System.Collections.Generic;

namespace Acadify.Models
{
    public class Form3ViewModel
    {
        public int MeetingId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentId { get; set; } = "";

        // Draft / Sent
        public string Status { get; set; } = "Draft";

        public List<Form3MeetingRowVM> Meetings { get; set; } = new();
        public string AdvisorNotes { get; set; } = "";
    }

    public class Form3MeetingRowVM
    {
        public int MeetingNo { get; set; }

        public string MeetingDate { get; set; } = ""; // مثال: Year/Fall/Spring/Summer

        // purpose
        public bool PurposeAcademic { get; set; }
        public bool PurposeCareer { get; set; }
        public bool PurposeOther { get; set; }

        // referral
        public string ReferralName { get; set; } = "";
        public string ReferralReason { get; set; } = "";

        // notes
        public string ProposedSolutions { get; set; } = "";

        public string StudentInitial { get; set; } = "";
        public string AdvisorInitial { get; set; } = "";
    }
}