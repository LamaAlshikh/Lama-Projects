using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("Community")]
public partial class Community
{
    [Key]
    [Column("communityID")]
    public int CommunityId { get; set; }

    [Required]
    [Column("communityName")]
    [StringLength(100)]
    public string CommunityName { get; set; } = null!;

    public virtual ICollection<CommunityMessage> CommunityMessages { get; set; } = new List<CommunityMessage>();
}