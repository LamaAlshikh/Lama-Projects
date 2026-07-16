using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//------00
namespace WepApp2.Models;

public partial class Course
{
    public int CourseID { get; set; }

    public string CourseName { get; set; } = null!;

    public string CourseField { get; set; } = null!;

    public string CourseDescription { get; set; } = null!;

    public string PresenterName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public int? ServiceId { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual Service? Service { get; set; }
}
