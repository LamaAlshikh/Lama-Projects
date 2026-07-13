using System;
using System.Collections.Generic;

namespace Acadify.Models.AdminPages
{
    public class AdvisorSentFormsVM
    {
        public int TotalForms { get; set; }
        public List<AdvisorSentFormsGroupVM> AdvisorGroups { get; set; } = new();
    }

    public class AdvisorSentFormsGroupVM
    {
        public int? AdvisorId { get; set; }
        public string AdvisorName { get; set; } = "";
        public string AdvisorEmail { get; set; } = "";
        public int TotalForms { get; set; }
        public List<AdvisorSentFormRowVM> Forms { get; set; } = new();
    }

    public class AdvisorSentFormRowVM
    {
        public int FormId { get; set; }
        public string FormType { get; set; } = "";
        public string FormTitle { get; set; } = "";
        public string FormStatus { get; set; } = "";

        public int? StudentId { get; set; }
        public string StudentName { get; set; } = "";

        public DateTime SentDate { get; set; }
        public string SentDateText { get; set; } = "";
        public string SentTimeText { get; set; } = "";

        public string ViewUrl { get; set; } = "";
        public string PrintUrl { get; set; } = "";
    }
}