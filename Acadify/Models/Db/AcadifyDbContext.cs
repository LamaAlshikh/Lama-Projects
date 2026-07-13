using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db;

public partial class AcadifyDbContext : DbContext
{
    public AcadifyDbContext()
    {
    }

    public AcadifyDbContext(DbContextOptions<AcadifyDbContext> options)
        : base(options)
    {
    }

    // --- مجموعات البيانات (DbSets) ---
    public virtual DbSet<Admin> Admins { get; set; }
    public virtual DbSet<AcademicAdvisingConfirmationForm> AcademicAdvisingConfirmationForms { get; set; }
    public virtual DbSet<AcademicCalendar> AcademicCalendars { get; set; }
    public virtual DbSet<AcademicCalendarEvent> AcademicCalendarEvents { get; set; }
    public virtual DbSet<Advisor> Advisors { get; set; }
    public virtual DbSet<AdvisorRequest> AdvisorRequests { get; set; }
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

    // إضافات رهف ولينا
    public virtual DbSet<StudyPlanCourse> StudyPlanCourses { get; set; }
    public virtual DbSet<CourseChoiceMonitoringForm> CourseChoiceMonitoringForms { get; set; }
    public virtual DbSet<TranscriptCourseDecision> TranscriptCourseDecisions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:AcadifyDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasMany(t => t.Courses)
                .WithMany(c => c.Transcripts)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseTranscript",
                    right => right
                        .HasOne<Course>()
                        .WithMany()
                        .HasForeignKey("CoursesCourseId")
                        .HasPrincipalKey(c => c.CourseId)
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Transcript>()
                        .WithMany()
                        .HasForeignKey("TranscriptsTranscriptId")
                        .HasPrincipalKey(t => t.TranscriptId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("CourseTranscript");

                        join.HasKey("CoursesCourseId", "TranscriptsTranscriptId");

                        join.IndexerProperty<string>("CoursesCourseId")
                            .HasColumnName("CoursesCourseId")
                            .HasMaxLength(30);

                        join.IndexerProperty<int>("TranscriptsTranscriptId")
                            .HasColumnName("TranscriptsTranscriptId");
                    });
        });// 1. StudyPlanCourse
        modelBuilder.Entity<StudyPlanCourse>(entity =>
        {
            entity.ToTable("StudyPlanCourse");

            entity.HasKey(e => new { e.PlanId, e.CourseId })
                .HasName("PK_StudyPlanCourse");

            entity.Property(e => e.PlanId)
                .HasColumnName("planID");

            entity.Property(e => e.CourseId)
                .HasMaxLength(30)
                .HasColumnName("courseID");

            entity.Property(e => e.SemesterNo)
                .HasColumnName("semesterNo");

            entity.Property(e => e.DisplayOrder)
                .HasColumnName("displayOrder");

            entity.HasOne<StudyPlan>()
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StudyPlanCourse_StudyPlan");

            entity.HasOne<Course>()
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StudyPlanCourse_Course");
        });
        // Prevent EF from creating a default join table named CourseStudyPlan
        modelBuilder.Entity<StudyPlan>()
            .Ignore("Courses");

        modelBuilder.Entity<Course>()
            .Ignore("Plans");
        // StudyPlan
        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId)
                .HasName("PK_StudyPlan");

            entity.ToTable("StudyPlan");

            entity.Property(e => e.PlanId)
                .HasColumnName("planID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Major)
                .HasMaxLength(120)
                .HasColumnName("major");

            entity.Property(e => e.TotalHours)
                .HasColumnName("totalHours");

            entity.Property(e => e.PdfFile)
                .HasMaxLength(255)
                .HasColumnName("pdfFile");
        });
        // AdvisorRequest
        modelBuilder.Entity<AdvisorRequest>(entity =>
        {
            entity.ToTable("AdvisorRequest");

            entity.HasKey(e => e.RequestId)
                .HasName("PK_AdvisorRequest");

            entity.Property(e => e.RequestId)
                .HasColumnName("requestID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.RequestedAdvisorId)
                .HasColumnName("requestedAdvisorID");

            entity.Property(e => e.RequestedAdvisorEmail)
                .HasMaxLength(150)
                .HasColumnName("requestedAdvisorEmail");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending")
                .HasColumnName("status");

            entity.Property(e => e.AdminNote)
                .HasMaxLength(300)
                .HasColumnName("adminNote");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("createdAt");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updatedAt");

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(e => e.RequestedAdvisor)
                .WithMany()
                .HasForeignKey(e => e.RequestedAdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
        // 2. AcademicCalendar
        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId)
                .HasName("PK_AcademicCalendar");

            entity.ToTable("AcademicCalendar");

            entity.Property(e => e.CalendarId)
                .HasColumnName("calendarID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.PdfFile)
                .HasMaxLength(255)
                .HasColumnName("pdfFile");

            entity.Property(e => e.UploadedAt)
                .HasColumnName("uploadedAt")
                .HasDefaultValueSql("(sysdatetime())");
        });
        modelBuilder.Entity<AcademicCalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK_AcademicCalendarEvent");

            entity.ToTable("AcademicCalendarEvent");

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.GregorianDate)
                .HasColumnName("gregorianDate")
                .HasColumnType("date");

            entity.Property(e => e.HijriDate)
                .HasMaxLength(20)
                .HasColumnName("hijriDate");

            entity.Property(e => e.DayAr)
                .HasMaxLength(20)
                .HasColumnName("dayAr");

            entity.Property(e => e.EventName)
                .HasMaxLength(500)
                .HasColumnName("eventName");

            entity.Property(e => e.CalendarId)
                .HasColumnName("calendarID");

            entity.HasOne(e => e.AcademicCalendar)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CalendarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AcademicCalendarEvent_AcademicCalendar");
        });
        // Community
        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.CommunityId)
                .HasName("PK_Community");

            entity.ToTable("Community");

            entity.Property(e => e.CommunityId)
                .HasColumnName("communityID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CommunityName)
                .HasMaxLength(100)
                .HasColumnName("communityName");
        });

        // CommunityMessage
        modelBuilder.Entity<CommunityMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId)
                .HasName("PK_CommunityMessage");

            entity.ToTable("CommunityMessages");

            entity.Property(e => e.MessageId)
                .HasColumnName("messageID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CommunityId)
                .HasColumnName("communityID");

            entity.Property(e => e.SenderName)
                .HasMaxLength(120)
                .HasColumnName("senderName");

            entity.Property(e => e.MessageText)
                .HasColumnName("messageText");

            entity.Property(e => e.MessageDate)
                .HasColumnName("messageDate");

            entity.HasOne(e => e.Community)
                .WithMany(c => c.CommunityMessages)
                .HasForeignKey(e => e.CommunityId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CommunityMessage_Community");
        });

        // 2. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_User");
            entity.ToTable("User");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("Name");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
        });
        // 2. admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId)
                .HasName("PK_Admin");

            entity.ToTable("Admin");

            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("UQ_Admin_UserID");

            entity.Property(e => e.AdminId)
    .HasColumnName("adminID")
    .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("userID");

            entity.HasOne(e => e.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_User");
        });
        // 3. Advisor
        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId)
                .HasName("PK__Advisor__D0081275B285C8F3");

            entity.ToTable("Advisor");

            entity.Property(e => e.AdvisorId)
                .ValueGeneratedNever()
                .HasColumnName("advisorID");

            entity.Property(e => e.UserId)
                .HasColumnName("userID");

            entity.Property(e => e.Department)
                .HasMaxLength(120)
                .HasColumnName("department");

            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("UQ_Advisor_UserID");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Advisor)
                .HasForeignKey<Advisor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Advisor_User");
        });
        // GraduationStatus
        modelBuilder.Entity<GraduationStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId)
                .HasName("PK_GraduationStatus");

            entity.ToTable("GraduationStatus");

            entity.HasIndex(e => e.StudentId)
                .IsUnique()
                .HasDatabaseName("UQ_GraduationStatus_Student");

            entity.Property(e => e.StatusId)
                .HasColumnName("statusID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.Status)
                .HasMaxLength(80)
                .HasColumnName("status");

            entity.Property(e => e.RemainingHours)
                .HasColumnName("remainingHours");

            entity.HasOne(e => e.Student)
                .WithOne(s => s.GraduationStatus)
                .HasForeignKey<GraduationStatus>(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_GraduationStatus_Student");
        });
        // 4. Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65C8C8F6A12");
            entity.ToTable("Student");

            entity.Property(e => e.StudentId)
                .ValueGeneratedNever()
                .HasColumnName("studentID");

            entity.Property(e => e.Major)
                .HasMaxLength(120)
                .HasColumnName("major");

            entity.Property(e => e.Name)
                .HasMaxLength(120);

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Students)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Advisor");
        });

        // 5. MatchingStatus
        // هذا الجزء يحل مشكلة علاقة Student مع MatchingStatus
        modelBuilder.Entity<MatchingStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.ToTable("MatchingStatus");

            entity.Property(e => e.StatusId)
                .HasColumnName("statusID");

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.Status)
                .HasMaxLength(60)
                .HasColumnName("status");

            entity.HasOne(e => e.Student)
                .WithOne(s => s.MatchingStatus)
                .HasForeignKey<MatchingStatus>(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MatchingStatus_Student");
        });

        // 6. Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__2AA84FF1285E5A7F");
            entity.ToTable("Course");

            entity.Property(e => e.CourseId)
                .HasMaxLength(30)
                .HasColumnName("courseID");

            entity.Property(e => e.CourseName)
                .HasMaxLength(200)
                .HasColumnName("courseName");

            entity.Property(e => e.RequirementCategory)
                .HasMaxLength(50)
                .HasColumnName("RequirementCategory");

            entity.Property(e => e.Hours)
                .HasColumnName("hours");

            entity.Property(e => e.Prerequisite)
                .HasMaxLength(200)
                .HasColumnName("prerequisite");
        });

        // 7. Forms
        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Forms__51BCB7CB3C44586F");

            entity.Property(e => e.FormId)
                .HasColumnName("formID");

            entity.Property(e => e.FormStatus)
                .HasMaxLength(60)
                .HasDefaultValue("Pending")
                .HasColumnName("formStatus");

            entity.Property(e => e.FormType)
                .HasMaxLength(80)
                .HasColumnName("formType");

            entity.Property(e => e.FormDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("formDate");

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Forms)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Student)
                .WithMany(p => p.Forms)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Forms_Student");
        });
        // NextSemesterCourseSelectionForm - Form 2
        modelBuilder.Entity<NextSemesterCourseSelectionForm>(entity =>
        {
            entity.HasKey(e => e.FormId)
                .HasName("PK_NextSemesterCourseSelectionForm");

            entity.ToTable("NextSemesterCourseSelectionForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.RecommendedCourses)
                .HasColumnName("recommendedCourses");

            entity.Property(e => e.RecommendedHours)
                .HasColumnName("recommendedHours");

            entity.Property(e => e.TrackChoice)
                .HasMaxLength(100)
                .HasColumnName("trackChoice");

            entity.Property(e => e.GpaChange)
                .HasMaxLength(100)
                .HasColumnName("gpaChange");

            entity.Property(e => e.PrerequisiteViolation)
                .HasColumnName("prerequisiteViolation");

            entity.HasOne(e => e.Form)
                .WithOne(f => f.NextSemesterCourseSelectionForm)
                .HasForeignKey<NextSemesterCourseSelectionForm>(e => e.FormId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NextSemesterCourseSelectionForm_Form");
        });

        // 8. AcademicAdvisingConfirmationForm - Form 1
        modelBuilder.Entity<AcademicAdvisingConfirmationForm>(entity =>
        {
            entity.HasKey(e => e.FormId)
                .HasName("PK_AcademicAdvisingConfirmationForm");

            entity.ToTable("AcademicAdvisingConfirmationForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.StudentName)
                .HasMaxLength(120)
                .HasColumnName("studentName");

            entity.Property(e => e.StudentLevel)
                .HasMaxLength(50)
                .HasColumnName("studentLevel");

            entity.Property(e => e.CurrentGpa)
                .HasColumnType("decimal(4,2)")
                .HasColumnName("currentGPA");

            entity.Property(e => e.CoursesCount)
                .HasColumnName("coursesCount");

            entity.HasOne(e => e.Form)
                .WithOne(f => f.AcademicAdvisingConfirmationForm)
                .HasForeignKey<AcademicAdvisingConfirmationForm>(e => e.FormId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AcademicAdvisingConfirmationForm_Form");
        });
        // Meeting
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.MeetingId)
                .HasName("PK_Meeting");

            entity.ToTable("Meeting");

            entity.Property(e => e.MeetingId)
                .HasColumnName("meetingID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.AdvisorId)
                .HasColumnName("advisorID");

            entity.Property(e => e.ChatRecord)
                .HasColumnName("chatRecord");

            entity.Property(e => e.ChatSummary)
                .HasColumnName("chatSummary");

            entity.Property(e => e.StartTime)
                .HasColumnName("startTime");

            entity.Property(e => e.EndTime)
                .HasColumnName("endTime");

            entity.Property(e => e.IsRecordingStarted)
                .HasColumnName("isRecordingStarted");

            entity.Property(e => e.LastRecordingAction)
                .HasMaxLength(100)
                .HasColumnName("lastRecordingAction");

            entity.Property(e => e.RecordingStartedAt)
                .HasColumnName("recordingStartedAt");

            entity.Property(e => e.RecordingStoppedAt)
                .HasColumnName("recordingStoppedAt");

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Meetings)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Student");

            entity.HasOne(e => e.Advisor)
                .WithMany(a => a.Meetings)
                .HasForeignKey(e => e.AdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Advisor");
        });

        // 8. StudyPlanMatchingForm
        modelBuilder.Entity<StudyPlanMatchingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__StudyPla__51BCB7CBBA7C5B63");
            entity.ToTable("StudyPlanMatchingForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.UniversityHours).HasColumnName("universityHours");
            entity.Property(e => e.PrepYearHours).HasColumnName("prepYearHours");
            entity.Property(e => e.FreeCoursesHours).HasColumnName("freeCoursesHours");
            entity.Property(e => e.CollegeMandatoryHours).HasColumnName("collegeMandatoryHours");
            entity.Property(e => e.DeptMandatoryHours).HasColumnName("deptMandatoryHours");
            entity.Property(e => e.DeptElectiveHours).HasColumnName("deptElectiveHours");
            entity.Property(e => e.TotalHours).HasColumnName("totalHours");

            entity.HasOne(d => d.Form)
                .WithOne(p => p.StudyPlanMatchingForm)
                .HasForeignKey<StudyPlanMatchingForm>(d => d.FormId);
        });

        // 9. CourseChoiceMonitoringForm
        modelBuilder.Entity<CourseChoiceMonitoringForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK_CourseChoiceMonitoringForm");
            entity.ToTable("CourseChoiceMonitoringForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.SelectedCoursesJson)
                .HasColumnName("selectedCoursesJson");

            entity.HasOne(d => d.Form)
                .WithOne(p => p.CourseChoiceMonitoringForm)
                .HasForeignKey<CourseChoiceMonitoringForm>(d => d.FormId);
        });
        // 10. TranscriptCourseDecision
        modelBuilder.Entity<TranscriptCourseDecision>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PK_TranscriptCourseDecision");

            entity.ToTable("TranscriptCourseDecision");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.TranscriptCourseId)
                .HasMaxLength(30)
                .HasColumnName("transcriptCourseID");

            entity.Property(e => e.DecisionType)
                .HasMaxLength(50)
                .HasColumnName("decisionType");

            entity.Property(e => e.EquivalentCourseId)
                .HasMaxLength(30)
                .HasColumnName("equivalentCourseID");

            entity.Property(e => e.IsApprovedByAdvisor)
                .HasColumnName("isApprovedByAdvisor");

            entity.Property(e => e.Notes)
                .HasColumnName("notes");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("createdAt");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.TranscriptCourseDecisions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TranscriptCourseDecision_Student");

            entity.HasOne(d => d.TranscriptCourse)
                .WithMany()
                .HasForeignKey(d => d.TranscriptCourseId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TranscriptCourseDecision_TranscriptCourse");

            entity.HasOne(d => d.EquivalentCourse)
                .WithMany()
                .HasForeignKey(d => d.EquivalentCourseId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TranscriptCourseDecision_EquivalentCourse");
        });
        // GraduationProjectEligibilityForm - Form 5
        modelBuilder.Entity<GraduationProjectEligibilityForm>(entity =>
        {
            entity.HasKey(e => e.FormId)
                .HasName("PK_GraduationProjectEligibilityForm");

            entity.ToTable("GraduationProjectEligibilityForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.Eligibility)
                .HasMaxLength(50)
                .HasColumnName("eligibility");

            entity.Property(e => e.RequiredCoursesStatus)
                .HasMaxLength(200)
                .HasColumnName("requiredCoursesStatus");

            entity.HasOne(e => e.Form)
                .WithOne(f => f.GraduationProjectEligibilityForm)
                .HasForeignKey<GraduationProjectEligibilityForm>(e => e.FormId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_GraduationProjectEligibilityForm_Form");
        });
        // MeetingForm - Form 3
        modelBuilder.Entity<MeetingForm>(entity =>
        {
            entity.HasKey(e => e.FormId)
                .HasName("PK_MeetingForm");

            entity.ToTable("MeetingForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");

            entity.Property(e => e.MeetingId)
                .HasColumnName("meetingID");

            entity.Property(e => e.MeetingStart)
                .HasColumnName("meetingStart");

            entity.Property(e => e.MeetingEnd)
                .HasColumnName("meetingEnd");

            entity.Property(e => e.MeetingPurpose)
                .HasMaxLength(100)
                .HasColumnName("meetingPurpose");

            entity.Property(e => e.MeetingNotes)
                .HasColumnName("meetingNotes");

            entity.Property(e => e.ReferralReason)
                .HasColumnName("referralReason");

            entity.Property(e => e.ReferredTo)
                .HasMaxLength(120)
                .HasColumnName("referredTo");

            entity.Property(e => e.StudentActions)
                .HasColumnName("studentActions");

            entity.Property(e => e.AdvisorActions)
                .HasColumnName("advisorActions");

            entity.HasOne(e => e.Form)
                .WithOne(f => f.MeetingForm)
                .HasForeignKey<MeetingForm>(e => e.FormId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MeetingForm_Form");
        });// MeetingMessage
        modelBuilder.Entity<MeetingMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId)
                .HasName("PK_MeetingMessages");

            entity.ToTable("MeetingMessages");

            entity.Property(e => e.MessageId)
                .HasColumnName("messageID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.MeetingId)
                .HasColumnName("meetingID");

            entity.Property(e => e.SenderName)
                .HasMaxLength(120)
                .HasColumnName("senderName");

            entity.Property(e => e.MessageText)
                .HasColumnName("messageText");

            entity.Property(e => e.MessageDate)
                .HasColumnName("messageDate");

            entity.Property(e => e.IsRecorded)
                .HasColumnName("isRecorded");
        });
        // 11. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId)
                .HasName("PK__Notifica__4BA5CE8975CAE89B");

            entity.ToTable("Notification");

            entity.HasIndex(e => e.StudentId).HasDatabaseName("IX_Notif_StudentID");
            entity.HasIndex(e => e.AdvisorId).HasDatabaseName("IX_Notif_AdvisorID");
            entity.HasIndex(e => e.AdminId).HasDatabaseName("IX_Notif_AdminID");

            entity.Property(e => e.NotificationId)
                .HasColumnName("notificationID");

            entity.Property(e => e.Message)
                .HasColumnName("message");

            entity.Property(e => e.Date)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("date");

            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");

            entity.Property(e => e.SenderRole)
                .HasMaxLength(50)
                .HasColumnName("senderRole");

            entity.Property(e => e.SourceType)
                .HasMaxLength(50)
                .HasColumnName("sourceType");

            entity.Property(e => e.AdvisorId)
                .HasColumnName("advisorID");

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.AdminId)
                .HasColumnName("adminID");

            entity.Property(e => e.IsRead)
                .HasColumnName("isRead");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Admin)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // 12. View: VwMyStudent
        modelBuilder.Entity<VwMyStudent>(entity =>
        {
            entity.HasNoKey().ToView("vw_MyStudents");

            entity.Property(e => e.StudentId)
                .HasColumnName("studentID");

            entity.Property(e => e.StudentName)
                .HasMaxLength(120)
                .HasColumnName("studentName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}