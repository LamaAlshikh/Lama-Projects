using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db
{
    [Table("Admin")]
    [Index("UserId", Name = "UQ_Admin_UserID", IsUnique = true)]
    public partial class Admin
    {
        [Key]
        [Column("adminID")]
        public int AdminId { get; set; }

        [Column("userID")]
        public int UserId { get; set; }

        // --- Navigation Properties ---

        [ForeignKey("UserId")]
        [InverseProperty("Admin")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}