using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WepApp2.Models;


namespace WepApp2.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BookingDevice> BookingDevices { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<ConsultationMajor> ConsultationMajors { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceLoan> DeviceLoans { get; set; }

    public virtual DbSet<LabVisit> LabVisits { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Technology> Technologies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VisitsDetail> VisitsDetails { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Arabic_CI_AI");

        modelBuilder.Entity<BookingDevice>(entity =>
        {
            entity.HasKey(e => e.BookingDeviceID).HasName("PK__BookingD__6263A24908874900");

            entity.Property(e => e.BookingDeviceID).HasColumnName("BookingDeviceID");
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.Faculty).HasMaxLength(255);
            entity.Property(e => e.ProjectName).HasMaxLength(255);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Device).WithMany(p => p.BookingDevices)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_BookingDevices_Devices");

            entity.HasOne(d => d.Request).WithMany(p => p.BookingDevices)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_BookingDevices_Requests");

            entity.HasOne(d => d.Service).WithMany(p => p.BookingDevices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_BookingDevices_Services");
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.ConsultationID).HasName("PK__Consulta__5D014A78858DB694");

            entity.Property(e => e.ConsultationID)
                .ValueGeneratedNever()
                .HasColumnName("ConsultationID");//modified---
            entity.Property(e => e.ConsultationMajorId).HasColumnName("ConsultationMajorID");
            entity.Property(e => e.PreferredContactMethod).HasMaxLength(255);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.ConsultationMajor).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.ConsultationMajorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Consultations_ConsultationMajors");

            entity.HasOne(d => d.Request).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_Consultations_Request");

            entity.HasOne(d => d.Service).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_Consultations_Services");
        });

        modelBuilder.Entity<ConsultationMajor>(entity =>
        {
            entity.HasKey(e => e.ConsultationMajorID).HasName("PK__Consulta__C757E54FDAE44A16");

            entity.Property(e => e.ConsultationMajorID).HasColumnName("ConsultationMajorID");//modified
            entity.Property(e => e.Major).HasMaxLength(255);
            entity.Property(e => e.ConsultationDescription).HasMaxLength(255);

        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseID).HasName("PK__Courses__C92D7187281C46B9");

            entity.Property(e => e.CourseID).HasColumnName("CourseID");
            entity.Property(e => e.CourseField).HasMaxLength(255);
            entity.Property(e => e.CourseName).HasMaxLength(255);
            entity.Property(e => e.PresenterName).HasMaxLength(255);
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Service).WithMany(p => p.Courses)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_Courses_Services");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceID).HasName("PK__Devices__49E12331AE3549DC");

            entity.Property(e => e.DeviceID).HasColumnName("DeviceID");
            entity.Property(e => e.BrandName).HasMaxLength(255);
            entity.Property(e => e.DeviceLocation).HasMaxLength(255);
            entity.Property(e => e.DeviceModel).HasMaxLength(255);
            entity.Property(e => e.DeviceName).HasMaxLength(255);
            entity.Property(e => e.DeviceStatus).HasMaxLength(255);
            entity.Property(e => e.LastMaintenance).HasColumnType("datetime");
            entity.Property(e => e.LastUpdate).HasColumnType("datetime");
            entity.Property(e => e.SerialNumber).HasMaxLength(255);
            entity.Property(e => e.TechnologyId).HasColumnName("TechnologyID");


            

            entity.HasOne(d => d.Technology).WithMany(p => p.Devices)
                .HasForeignKey(d => d.TechnologyId)
                .HasConstraintName("FK_Devices_Technologies");

        });

        modelBuilder.Entity<DeviceLoan>(entity =>
        {
            entity.HasKey(e => e.DeviceLoanID).HasName("PK__DeviceLo__385CB179A85FB7C9");

            entity.Property(e => e.DeviceLoanID).HasColumnName("DeviceLoanID");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.PreferredContactMethod).HasMaxLength(255);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceLoans)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_DeviceLoans_Devices");

            entity.HasOne(d => d.Request).WithMany(p => p.DeviceLoans)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_DeviceLoans_Requests");

            entity.HasOne(d => d.Service).WithMany(p => p.DeviceLoans)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_DeviceLoans_Services");
        });

        modelBuilder.Entity<LabVisit>(entity =>
        {
            entity.HasKey(e => e.LabVisitID).HasName("PK__LabVisit__DDA12342E18829A7");

            //entity.Property(e => e.LabVisitID).HasColumnName("LabVisitID");//del
            entity.Property(e => e.NumberOfVisitors).HasDefaultValue(1);
            entity.Property(e => e.PreferredContactMethod).HasMaxLength(255);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.VisitDetailsId).HasColumnName("VisitDetailsID");

            entity.HasOne(d => d.Request).WithMany(p => p.LabVisits)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_LabVisits_Requests");

            entity.HasOne(d => d.Service).WithMany(p => p.LabVisits)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_LabVisits_Services");

            entity.HasOne(d => d.VisitDetails).WithMany(p => p.LabVisits)
                .HasForeignKey(d => d.VisitDetailsId)
                .HasConstraintName("FK_LabVisits_VisitsDetails");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestID).HasName("PK__Requests__33A8519A66055425");

            entity.Property(e => e.RequestID).HasColumnName("RequestID");
            entity.Property(e => e.AdminStatus).HasMaxLength(255);
            entity.Property(e => e.CourseID).HasColumnName("CourseID");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.RequestDate).HasColumnType("datetime");
            entity.Property(e => e.RequestType).HasMaxLength(255);
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SupervisorStatus)
                .HasMaxLength(255)
                .HasDefaultValue("بإنتظار إسناد المشرف");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Course).WithMany(p => p.Requests)
                .HasForeignKey(d => d.CourseID)
                .HasConstraintName("FK_Request_Courses");

            entity.HasOne(d => d.Device).WithMany(p => p.Requests)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Request_Devices");

            entity.HasOne(d => d.Service).WithMany(p => p.Requests)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Request_Services");

            entity.HasOne(d => d.User).WithMany(p => p.Requests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Request_Users");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceID).HasName("PK__Services__C51BB0EAF5B6E17D");

            entity.Property(e => e.ServiceID).HasColumnName("ServiceID");
            entity.Property(e => e.ServiceName).HasMaxLength(255);
        });

        modelBuilder.Entity<Technology>(entity =>
        {
            entity.HasKey(e => e.TechnologyID).HasName("PK__Technolo__705701783492A34E");

            entity.Property(e => e.TechnologyID).HasColumnName("TechnologyID");
            entity.Property(e => e.TechnologyName)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.TechnologyDescription)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false);
        });///modified--


        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserID).HasName("PK__Users__1788CCAC15D188DC");

            entity.HasIndex(e => e.PhoneNumber, "UQ__Users__85FB4E38F82DB5BA").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534374EE266").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ__Users__C9F2845697958F24").IsUnique();

            entity.Property(e => e.UserID).HasColumnName("UserID");
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Faculty).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogIn).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(255);
            entity.Property(e => e.UserName).HasMaxLength(255);
            entity.Property(e => e.UserPassWord).HasMaxLength(255);
            entity.Property(e => e.UserRole).HasMaxLength(255);
        });

        modelBuilder.Entity<VisitsDetail>(entity =>
        {
            entity.HasKey(e => e.VisitDetailsID).HasName("PK__VisitsDe__BCA58B13ED325902");

            entity.Property(e => e.VisitDetailsID).HasColumnName("VisitDetailsID");
            entity.Property(e => e.VisitType)
                .HasMaxLength(255)
                .HasColumnName("visitType");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
