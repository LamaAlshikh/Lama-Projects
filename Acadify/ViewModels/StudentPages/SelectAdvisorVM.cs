using Acadify.Models;
using System.ComponentModel.DataAnnotations;

namespace Acadify.Models.StudentPages
{
    public class SelectAdvisorVM
    {
        public string StudentName { get; set; } = string.Empty;

        public List<AdvisorCardVM> Advisors { get; set; } = new();

        public bool HasPendingRequest { get; set; }
        public string? PendingAdvisorEmail { get; set; }
        public string? PendingStatus { get; set; }
        public string? Message { get; set; }

        [EmailAddress]
        public string? ManualAdvisorEmail { get; set; }

        public string SearchTerm { get; set; } = string.Empty;
    }

    public class AdvisorCardVM
    {
        public int AdvisorId { get; set; }
        public string AdvisorName { get; set; } = string.Empty;
        public string AdvisorEmail { get; set; } = string.Empty;
        public string? Department { get; set; }
    }
}