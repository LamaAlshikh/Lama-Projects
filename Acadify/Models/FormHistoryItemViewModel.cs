namespace Acadify.Models
{
    public class FormHistoryItemViewModel
    {
        public int FormId { get; set; }
        public string FormTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public string DateText { get; set; } = "";
        public string ViewUrl { get; set; } = "";
    }
}