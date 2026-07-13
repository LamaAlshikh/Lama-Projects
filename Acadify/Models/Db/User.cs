using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db;

[Table("User")]
[Index("Email", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("userID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [InverseProperty("User")]
    public virtual Admin? Admin { get; set; }

    [InverseProperty("User")]
    public virtual Advisor? Advisor { get; set; }

    [InverseProperty("User")]
    public virtual Student? Student { get; set; }
}