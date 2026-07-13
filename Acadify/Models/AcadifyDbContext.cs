using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

public partial class AcadifyDbContext : DbContext
{
    public AcadifyDbContext() { }

    public AcadifyDbContext(DbContextOptions<AcadifyDbContext> options) : base(options) { }

    // DbSets
    public virtual DbSet<AcademicAdvisingConfirmationForm> AcademicAdvisingConfirmationForms { get; set; }
    public virtual DbSet<AcademicCalendar> AcademicCalendars { get; set; }
    public virtual DbSet<AcademicCalendarEvent> AcademicCalendarEvents { get; set; }
    public virtual DbSet<Admin> Admins { get; set; }
    public virtual DbSet<Advisor> Advisors { get; set; }
    public virtual DbSet<AdvisorRequest> AdvisorRequests { get; set; } // تم التأكيد على وجوده
    public virtual DbSet<Community> Communities { get; set; }
    public virtual DbSet<CommunityMessage> CommunityMessages { get; set; }
    public virtual DbSet<Course> Courses { get; set; }
    public virtual DbSet<Form> Forms { get; set; }
    public virtual DbSet<GraduationProjectEligibilityForm> GraduationProjectEligibilityForms { get; set; }
    public virtual DbSet<GraduationStatus> GraduationStatuses { get; set; }
    public virtual DbSet<MatchingStatus> MatchingStatuses { get; set; }
    public virtual DbSet<Meeting> Meetings { get; set; }
    public virtual DbSet<MeetingForm> MeetingForms { get; set; }
    public virtual DbSet<MeetingMessage> MeetingMessages { get; set; }
    public virtual DbSet<NextSemesterCourseSelectionForm> NextSemesterCourseSelectionForms { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<StudyPlan> StudyPlans { get; set; }
    public virtual DbSet<StudyPlanMatchingForm> StudyPlanMatchingForms { get; set; }
    public virtual DbSet<Transcript> Transcripts { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<VwMyStudent> VwMyStudents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Academic Calendar Configurations
        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId).HasName("PK__Academic__EE5496D6D3FAC23E");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<AcademicCalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GregorianDate).HasColumnType("date").IsRequired();
            entity.Property(e => e.HijriDate).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EventName).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.AcademicCalendar).WithMany(c => c.Events)
                  .HasForeignKey(e => e.CalendarId).OnDelete(DeleteBehavior.Cascade);
        });

        // 2. User Roles (Admin, Advisor, Student)
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF6994AC27");
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK_Admin");
            entity.HasOne(d => d.User).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId).HasName("PK__Advisor__D008127590DAB1D2");
            entity.HasOne(d => d.User).WithOne(p => p.Advisor)
                .HasForeignKey<Advisor>(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65C76ED7B60");
            entity.HasOne(d => d.Advisor).WithMany(p => p.Students)
                .HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.SetNull);
        });

        // 3. Advisor Requests Logic (NEW)
        modelBuilder.Entity<AdvisorRequest>(entity =>
        {
            entity.ToTable("AdvisorRequest");
            entity.HasKey(e => e.RequestId).HasName("PK_AdvisorRequest");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.RequestedAdvisor).WithMany()
                .HasForeignKey(d => d.RequestedAdvisorId);
        });

        // 4. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__4BA5CE89C66256A6");
            entity.Property(e => e.Date).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasOne(d => d.Student).WithMany(p => p.Notifications).OnDelete(DeleteBehavior.Cascade);
        });

        // 5. Views
        modelBuilder.Entity<VwMyStudent>(entity => { entity.ToView("vw_MyStudents"); });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}