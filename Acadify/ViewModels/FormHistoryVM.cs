// ViewModels/FormHistoryVM.cs
using System;
using System.Collections.Generic;

namespace Acadify.ViewModels
{
    public class FormHistoryVM
    {
        public string FormType { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string PageTitle { get; set; } = string.Empty;
        public List<FormHistoryItemVM> Forms { get; set; } = new();
    }

    public class FormHistoryItemVM
    {
        public int FormId { get; set; }
        public string FormType { get; set; } = string.Empty;
        public string FormStatus { get; set; } = string.Empty;
        public DateTime FormDate { get; set; }
    }
}