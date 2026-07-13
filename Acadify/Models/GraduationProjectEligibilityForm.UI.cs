using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db
{
    public partial class GraduationProjectEligibilityForm
    {
        // UI-only (not saved in DB)
        [NotMapped] public string? StudentName { get; set; }
        [NotMapped] public string? StudentId { get; set; }

        [NotMapped] public bool CPIS351 { get; set; }
        [NotMapped] public bool CPIS358 { get; set; }
        [NotMapped] public bool CPIS323 { get; set; }

        [NotMapped] public bool CPIS360 { get; set; }
        [NotMapped] public bool CPIS375 { get; set; }
        [NotMapped] public bool CPIS342 { get; set; }

        [NotMapped] public bool IsEligible { get; set; }  // نخليه set عشان كنترولر يتحكم فيه
    }
}