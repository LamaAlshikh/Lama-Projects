using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WepApp2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Faculty = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PassWord = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LastLogIn = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC48D148C7", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "BookingDevices",
                columns: table => new
                {
                    BookingDevices = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProjectDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Faculty = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    DeviceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BookingD__EDD3255410E4623A", x => x.BookingDevices);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationMajors",
                columns: table => new
                {
                    ConsultationMajorID = table.Column<int>(type: "int", nullable: false),
                    Major = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsultationID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Consulta__C757E54FCA8BDCBC", x => x.ConsultationMajorID);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    ConsultationID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CommunicationMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    AvailableTimes = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Consulta__5D014A7847927F7B", x => x.ConsultationID);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CourseField = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CourseDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PresenterName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SeatCapacity = table.Column<int>(type: "int", nullable: true),
                    CourseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Courses__C92D718765CDAE24", x => x.CourseID);
                });

            migrationBuilder.CreateTable(
                name: "CoursesRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CoursesR__5F3A8682B362A6F7", x => new { x.RequestID, x.CourseID });
                    table.ForeignKey(
                        name: "FK__CoursesRe__Cours__6D0D32F4",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                });

            migrationBuilder.CreateTable(
                name: "DeviceLoans",
                columns: table => new
                {
                    LoanID = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    DeviceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DeviceLo__4F5AD4374F130F35", x => x.LoanID);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceID = table.Column<int>(type: "int", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LastMaintenance = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Devices__49E123315D92FBB2", x => x.DeviceID);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    TechnologyID = table.Column<int>(type: "int", nullable: false),
                    TechnologyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Technolo__705701784316E071", x => x.TechnologyID);
                    table.ForeignKey(
                        name: "FK_Technologies_Devices",
                        column: x => x.DeviceID,
                        principalTable: "Devices",
                        principalColumn: "DeviceID");
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnologyID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Services__C51BB0EACFB7ECCB", x => x.ServiceID);
                    table.ForeignKey(
                        name: "FK_Services_Technologies",
                        column: x => x.TechnologyID,
                        principalTable: "Technologies",
                        principalColumn: "TechnologyID");
                });

            migrationBuilder.CreateTable(
                name: "Request",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SupervisorStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    DeviceID = table.Column<int>(type: "int", nullable: true),
                    SupervisorAssigned = table.Column<int>(type: "int", nullable: false),
                    AdminStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Request__33A8519A6244A6D5", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_Request_Devices",
                        column: x => x.DeviceID,
                        principalTable: "Devices",
                        principalColumn: "DeviceID");
                    table.ForeignKey(
                        name: "FK_Request_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                    table.ForeignKey(
                        name: "FK_Request_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "LabVisits",
                columns: table => new
                {
                    LabVisitID = table.Column<int>(type: "int", nullable: false),
                    NumberOfVisitors = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CommunicationMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LabVisit__DDA1234232DDD84C", x => x.LabVisitID);
                    table.ForeignKey(
                        name: "FK_LabVisits_Request",
                        column: x => x.RequestID,
                        principalTable: "Request",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_LabVisits_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "VisitsDetails",
                columns: table => new
                {
                    VisitsDetailsID = table.Column<int>(type: "int", nullable: false),
                    visitType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LabVisitID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VisitsDe__746A09CF6F218A8C", x => x.VisitsDetailsID);
                    table.ForeignKey(
                        name: "FK_VisitsDetails_LabVisits",
                        column: x => x.LabVisitID,
                        principalTable: "LabVisits",
                        principalColumn: "LabVisitID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDevices_DeviceID",
                table: "BookingDevices",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDevices_RequestID",
                table: "BookingDevices",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDevices_ServiceID",
                table: "BookingDevices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMajors_ConsultationID",
                table: "ConsultationMajors",
                column: "ConsultationID");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_RequestID",
                table: "Consultations",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ServiceID",
                table: "Consultations",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_RequestID",
                table: "Courses",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ServiceID",
                table: "Courses",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_CoursesRequests_CourseID",
                table: "CoursesRequests",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoans_DeviceID",
                table: "DeviceLoans",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoans_RequestID",
                table: "DeviceLoans",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLoans_ServiceID",
                table: "DeviceLoans",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ServiceID",
                table: "Devices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_LabVisits_RequestID",
                table: "LabVisits",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_LabVisits_ServiceID",
                table: "LabVisits",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Request_DeviceID",
                table: "Request",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_Request_ServiceID",
                table: "Request",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Request_UserID",
                table: "Request",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TechnologyID",
                table: "Services",
                column: "TechnologyID");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_DeviceID",
                table: "Technologies",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_VisitsDetails_LabVisitID",
                table: "VisitsDetails",
                column: "LabVisitID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDevices_Devices",
                table: "BookingDevices",
                column: "DeviceID",
                principalTable: "Devices",
                principalColumn: "DeviceID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDevices_Request",
                table: "BookingDevices",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDevices_Services",
                table: "BookingDevices",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationMajors_Consultations",
                table: "ConsultationMajors",
                column: "ConsultationID",
                principalTable: "Consultations",
                principalColumn: "ConsultationID");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_Request",
                table: "Consultations",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_Services",
                table: "Consultations",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Request",
                table: "Courses",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Services",
                table: "Courses",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK__CoursesRe__Reque__6C190EBB",
                table: "CoursesRequests",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLoans_Devices",
                table: "DeviceLoans",
                column: "DeviceID",
                principalTable: "Devices",
                principalColumn: "DeviceID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLoans_Request",
                table: "DeviceLoans",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLoans_Services",
                table: "DeviceLoans",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Services",
                table: "Devices",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Technologies_Devices",
                table: "Technologies");

            migrationBuilder.DropTable(
                name: "BookingDevices");

            migrationBuilder.DropTable(
                name: "ConsultationMajors");

            migrationBuilder.DropTable(
                name: "CoursesRequests");

            migrationBuilder.DropTable(
                name: "DeviceLoans");

            migrationBuilder.DropTable(
                name: "VisitsDetails");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "LabVisits");

            migrationBuilder.DropTable(
                name: "Request");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Technologies");
        }
    }
}
