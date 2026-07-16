using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WepApp2.Models;

public partial class User
{
    [Key]
    public int UserID { get; set; }

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string UserName { get; set; } = null!;

    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string UserRole { get; set; } = null!;

    public string Faculty { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string UserPassWord { get; set; } = null!;

    public DateTime LastLogIn { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    
    [NotMapped]
    public string? OtherFaculty { get; set; }
}
