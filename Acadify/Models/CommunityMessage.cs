using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("CommunityMessages")]
public partial class CommunityMessage
{
    [Key]
    [Column("messageID")]
    public int MessageId { get; set; }

    [Column("communityID")]
    public int CommunityId { get; set; }

    [Column("senderName")]
    [StringLength(120)]
    public string SenderName { get; set; } = null!;

    [Column("messageText")]
    public string MessageText { get; set; } = null!;

    [Column("messageDate")]
    public DateTime? MessageDate { get; set; }

    [ForeignKey(nameof(CommunityId))]
    [InverseProperty(nameof(Community.CommunityMessages))]
    public virtual Community Community { get; set; } = null!;
}
