using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

public partial class MeetingMessage
{
    [Key]
    [Column("messageID")]
    public int MessageId { get; set; }

    [Column("meetingID")]
    public int MeetingId { get; set; }

    [Column("senderName")]
    [StringLength(120)]
    public string SenderName { get; set; } = null!;

    [Column("messageText")]
    public string MessageText { get; set; } = null!;

    [Column("messageDate")]
    public DateTime? MessageDate { get; set; }
}
