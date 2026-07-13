using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models
{
    [Table("AdvisorRequest")]
    public class AdvisorRequest
    {
        [Key]
        [Column("requestID")]
        public int RequestId { get; set; }

        [Column("studentID")]
        public int StudentId { get; set; }

        [Column("requestedAdvisorID")]
        public int? RequestedAdvisorId { get; set; }

        [Column("requestedAdvisorEmail")]
        [StringLength(150)]
        public string? RequestedAdvisorEmail { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Column("adminNote")]
        [StringLength(300)]
        public string? AdminNote { get; set; }

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey("RequestedAdvisorId")]
        public virtual Advisor? RequestedAdvisor { get; set; }
    }
}