using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Community")]
public partial class Community
{
    [Key]
    [Column("communityID")]
    public int CommunityId { get; set; }

    [Column("communityName")]
    [StringLength(100)]
    public string CommunityName { get; set; } = null!;

    [InverseProperty("Community")]
    public virtual ICollection<CommunityMessage> CommunityMessages { get; set; } = new List<CommunityMessage>();
}
