namespace Acadify.Models
{
    public class StudentFormsVM
    {
        public int FormId { get; set; }
        public string FormTitle { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public bool CanSend { get; set; }
    }
}
