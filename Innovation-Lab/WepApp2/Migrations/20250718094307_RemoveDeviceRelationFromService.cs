using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WepApp2.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceRelationFromService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDevices_Request",
                table: "BookingDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationMajors_Consultations",
                table: "ConsultationMajors");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Request",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceLoans_Request",
                table: "DeviceLoans");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Services",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_LabVisits_Request",
                table: "LabVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Technologies",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Technologies_Devices",
                table: "Technologies");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitsDetails_LabVisits",
                table: "VisitsDetails");

            migrationBuilder.DropTable(
                name: "CoursesRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK__VisitsDe__746A09CF6F218A8C",
                table: "VisitsDetails");

            migrationBuilder.DropIndex(
                name: "IX_VisitsDetails_LabVisitID",
                table: "VisitsDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Users__1788CCAC48D148C7",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Technolo__705701784316E071",
                table: "Technologies");

            migrationBuilder.DropIndex(
                name: "IX_Technologies_DeviceID",
                table: "Technologies");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Services__C51BB0EACFB7ECCB",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_TechnologyID",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK__LabVisit__DDA1234232DDD84C",
                table: "LabVisits");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Devices__49E123315D92FBB2",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_ServiceID",
                table: "Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK__DeviceLo__4F5AD4374F130F35",
                table: "DeviceLoans");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Courses__C92D718765CDAE24",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_RequestID",
                table: "Courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Consulta__5D014A7847927F7B",
                table: "Consultations");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Consulta__C757E54FCA8BDCBC",
                table: "ConsultationMajors");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationMajors_ConsultationID",
                table: "ConsultationMajors");

            migrationBuilder.DropPrimaryKey(
                name: "PK__BookingD__EDD3255410E4623A",
                table: "BookingDevices");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Request__33A8519A6244A6D5",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "LabVisitID",
                table: "VisitsDetails");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "DeviceID",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "TechnologyID",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ServiceID",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "CourseDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RequestID",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SeatCapacity",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ConsultationID",
                table: "ConsultationMajors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ConsultationMajors");

            migrationBuilder.RenameTable(
                name: "Request",
                newName: "Requests");

            migrationBuilder.RenameColumn(
                name: "VisitsDetailsID",
                table: "VisitsDetails",
                newName: "VisitDetailsID");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "UserRole");

            migrationBuilder.RenameColumn(
                name: "PassWord",
                table: "Users",
                newName: "UserPassWord");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Services",
                newName: "ServiceDescription");

            migrationBuilder.RenameColumn(
                name: "CommunicationMethod",
                table: "LabVisits",
                newName: "PreferredContactMethod");

            migrationBuilder.RenameColumn(
                name: "LoanID",
                table: "DeviceLoans",
                newName: "DeviceLoanID");

            migrationBuilder.RenameColumn(
                name: "RequestedDate",
                table: "Consultations",
                newName: "ConsultationDate");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Consultations",
                newName: "ConsultationDescription");

            migrationBuilder.RenameColumn(
                name: "CommunicationMethod",
                table: "Consultations",
                newName: "PreferredContactMethod");

            migrationBuilder.RenameColumn(
                name: "BookingDevices",
                table: "BookingDevices",
                newName: "BookingDeviceID");

            migrationBuilder.RenameIndex(
                name: "IX_Request_UserID",
                table: "Requests",
                newName: "IX_Requests_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Request_ServiceID",
                table: "Requests",
                newName: "IX_Requests_ServiceID");

            migrationBuilder.RenameIndex(
                name: "IX_Request_DeviceID",
                table: "Requests",
                newName: "IX_Requests_DeviceID");

            migrationBuilder.AlterColumn<int>(
                name: "VisitDetailsID",
                table: "VisitsDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VisitsDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "TechnologyID",
                table: "Technologies",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Technologies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TechnologyDescription",
                table: "Technologies",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceID",
                table: "Services",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "VisitDate",
                table: "LabVisits",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<int>(
                name: "LabVisitID",
                table: "LabVisits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "VisitDetailsID",
                table: "LabVisits",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Devices",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMaintenance",
                table: "Devices",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceID",
                table: "Devices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Devices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TechnologyID",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceLoanID",
                table: "DeviceLoans",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "CourseID",
                table: "Courses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConsultationMajorID",
                table: "Consultations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ConsultationMajorID",
                table: "ConsultationMajors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "ConsultationDescription",
                table: "ConsultationMajors",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ConsultationMajors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "BookingDeviceID",
                table: "BookingDevices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupervisorStatus",
                table: "Requests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "بإنتظار إسناد المشرف",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "SupervisorAssigned",
                table: "Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceID",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceID",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminStatus",
                table: "Requests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "RequestID",
                table: "Requests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "CourseID",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__VisitsDe__BCA58B13ED325902",
                table: "VisitsDetails",
                column: "VisitDetailsID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Users__1788CCAC15D188DC",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Technolo__705701783492A34E",
                table: "Technologies",
                column: "TechnologyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Services__C51BB0EAF5B6E17D",
                table: "Services",
                column: "ServiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__LabVisit__DDA12342E18829A7",
                table: "LabVisits",
                column: "LabVisitID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Devices__49E12331AE3549DC",
                table: "Devices",
                column: "DeviceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__DeviceLo__385CB179A85FB7C9",
                table: "DeviceLoans",
                column: "DeviceLoanID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Courses__C92D7187281C46B9",
                table: "Courses",
                column: "CourseID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Consulta__5D014A78858DB694",
                table: "Consultations",
                column: "ConsultationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Consulta__C757E54FDAE44A16",
                table: "ConsultationMajors",
                column: "ConsultationMajorID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__BookingD__6263A24908874900",
                table: "BookingDevices",
                column: "BookingDeviceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Requests__33A8519A66055425",
                table: "Requests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__85FB4E38F82DB5BA",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534374EE266",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__C9F2845697958F24",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabVisits_VisitDetailsID",
                table: "LabVisits",
                column: "VisitDetailsID");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_TechnologyID",
                table: "Devices",
                column: "TechnologyID");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ConsultationMajorID",
                table: "Consultations",
                column: "ConsultationMajorID");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CourseID",
                table: "Requests",
                column: "CourseID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDevices_Requests",
                table: "BookingDevices",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_ConsultationMajors",
                table: "Consultations",
                column: "ConsultationMajorID",
                principalTable: "ConsultationMajors",
                principalColumn: "ConsultationMajorID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLoans_Requests",
                table: "DeviceLoans",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Technologies",
                table: "Devices",
                column: "TechnologyID",
                principalTable: "Technologies",
                principalColumn: "TechnologyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LabVisits_Requests",
                table: "LabVisits",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_LabVisits_VisitsDetails",
                table: "LabVisits",
                column: "VisitDetailsID",
                principalTable: "VisitsDetails",
                principalColumn: "VisitDetailsID");

            migrationBuilder.AddForeignKey(
                name: "FK_Request_Courses",
                table: "Requests",
                column: "CourseID",
                principalTable: "Courses",
                principalColumn: "CourseID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDevices_Requests",
                table: "BookingDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_ConsultationMajors",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceLoans_Requests",
                table: "DeviceLoans");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Technologies",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_LabVisits_Requests",
                table: "LabVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_LabVisits_VisitsDetails",
                table: "LabVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_Request_Courses",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK__VisitsDe__BCA58B13ED325902",
                table: "VisitsDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Users__1788CCAC15D188DC",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UQ__Users__85FB4E38F82DB5BA",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UQ__Users__A9D10534374EE266",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UQ__Users__C9F2845697958F24",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Technolo__705701783492A34E",
                table: "Technologies");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Services__C51BB0EAF5B6E17D",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK__LabVisit__DDA12342E18829A7",
                table: "LabVisits");

            migrationBuilder.DropIndex(
                name: "IX_LabVisits_VisitDetailsID",
                table: "LabVisits");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Devices__49E12331AE3549DC",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_TechnologyID",
                table: "Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK__DeviceLo__385CB179A85FB7C9",
                table: "DeviceLoans");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Courses__C92D7187281C46B9",
                table: "Courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Consulta__5D014A78858DB694",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_ConsultationMajorID",
                table: "Consultations");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Consulta__C757E54FDAE44A16",
                table: "ConsultationMajors");

            migrationBuilder.DropPrimaryKey(
                name: "PK__BookingD__6263A24908874900",
                table: "BookingDevices");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Requests__33A8519A66055425",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CourseID",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VisitsDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "TechnologyDescription",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "VisitDetailsID",
                table: "LabVisits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "TechnologyID",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ConsultationMajorID",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "ConsultationDescription",
                table: "ConsultationMajors");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ConsultationMajors");

            migrationBuilder.DropColumn(
                name: "CourseID",
                table: "Requests");

            migrationBuilder.RenameTable(
                name: "Requests",
                newName: "Request");

            migrationBuilder.RenameColumn(
                name: "VisitDetailsID",
                table: "VisitsDetails",
                newName: "VisitsDetailsID");

            migrationBuilder.RenameColumn(
                name: "UserRole",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "UserPassWord",
                table: "Users",
                newName: "PassWord");

            migrationBuilder.RenameColumn(
                name: "ServiceDescription",
                table: "Services",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "PreferredContactMethod",
                table: "LabVisits",
                newName: "CommunicationMethod");

            migrationBuilder.RenameColumn(
                name: "DeviceLoanID",
                table: "DeviceLoans",
                newName: "LoanID");

            migrationBuilder.RenameColumn(
                name: "PreferredContactMethod",
                table: "Consultations",
                newName: "CommunicationMethod");

            migrationBuilder.RenameColumn(
                name: "ConsultationDescription",
                table: "Consultations",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ConsultationDate",
                table: "Consultations",
                newName: "RequestedDate");

            migrationBuilder.RenameColumn(
                name: "BookingDeviceID",
                table: "BookingDevices",
                newName: "BookingDevices");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_UserID",
                table: "Request",
                newName: "IX_Request_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ServiceID",
                table: "Request",
                newName: "IX_Request_ServiceID");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_DeviceID",
                table: "Request",
                newName: "IX_Request_DeviceID");

            migrationBuilder.AlterColumn<int>(
                name: "VisitsDetailsID",
                table: "VisitsDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "LabVisitID",
                table: "VisitsDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PhoneNumber",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "TechnologyID",
                table: "Technologies",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Technologies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeviceID",
                table: "Technologies",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ServiceID",
                table: "Services",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "TechnologyID",
                table: "Services",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "VisitDate",
                table: "LabVisits",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "LabVisitID",
                table: "LabVisits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Devices",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMaintenance",
                table: "Devices",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceID",
                table: "Devices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Devices",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ServiceID",
                table: "Devices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LoanID",
                table: "DeviceLoans",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "CourseID",
                table: "Courses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CourseDate",
                table: "Courses",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Courses",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "RequestID",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatCapacity",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Courses",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AlterColumn<int>(
                name: "ConsultationMajorID",
                table: "ConsultationMajors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ConsultationID",
                table: "ConsultationMajors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ConsultationMajors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "BookingDevices",
                table: "BookingDevices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Request",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SupervisorStatus",
                table: "Request",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldDefaultValue: "بإنتظار إسناد المشرف");

            migrationBuilder.AlterColumn<int>(
                name: "SupervisorAssigned",
                table: "Request",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ServiceID",
                table: "Request",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Request",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceID",
                table: "Request",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AdminStatus",
                table: "Request",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestID",
                table: "Request",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK__VisitsDe__746A09CF6F218A8C",
                table: "VisitsDetails",
                column: "VisitsDetailsID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Users__1788CCAC48D148C7",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Technolo__705701784316E071",
                table: "Technologies",
                column: "TechnologyID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Services__C51BB0EACFB7ECCB",
                table: "Services",
                column: "ServiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__LabVisit__DDA1234232DDD84C",
                table: "LabVisits",
                column: "LabVisitID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Devices__49E123315D92FBB2",
                table: "Devices",
                column: "DeviceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__DeviceLo__4F5AD4374F130F35",
                table: "DeviceLoans",
                column: "LoanID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Courses__C92D718765CDAE24",
                table: "Courses",
                column: "CourseID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Consulta__5D014A7847927F7B",
                table: "Consultations",
                column: "ConsultationID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Consulta__C757E54FCA8BDCBC",
                table: "ConsultationMajors",
                column: "ConsultationMajorID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__BookingD__EDD3255410E4623A",
                table: "BookingDevices",
                column: "BookingDevices");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Request__33A8519A6244A6D5",
                table: "Request",
                column: "RequestID");

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
                    table.ForeignKey(
                        name: "FK__CoursesRe__Reque__6C190EBB",
                        column: x => x.RequestID,
                        principalTable: "Request",
                        principalColumn: "RequestID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitsDetails_LabVisitID",
                table: "VisitsDetails",
                column: "LabVisitID");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_DeviceID",
                table: "Technologies",
                column: "DeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TechnologyID",
                table: "Services",
                column: "TechnologyID");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ServiceID",
                table: "Devices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_RequestID",
                table: "Courses",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMajors_ConsultationID",
                table: "ConsultationMajors",
                column: "ConsultationID");

            migrationBuilder.CreateIndex(
                name: "IX_CoursesRequests_CourseID",
                table: "CoursesRequests",
                column: "CourseID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDevices_Request",
                table: "BookingDevices",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationMajors_Consultations",
                table: "ConsultationMajors",
                column: "ConsultationID",
                principalTable: "Consultations",
                principalColumn: "ConsultationID");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Request",
                table: "Courses",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceLoans_Request",
                table: "DeviceLoans",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Services",
                table: "Devices",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_LabVisits_Request",
                table: "LabVisits",
                column: "RequestID",
                principalTable: "Request",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Technologies",
                table: "Services",
                column: "TechnologyID",
                principalTable: "Technologies",
                principalColumn: "TechnologyID");

            migrationBuilder.AddForeignKey(
                name: "FK_Technologies_Devices",
                table: "Technologies",
                column: "DeviceID",
                principalTable: "Devices",
                principalColumn: "DeviceID");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitsDetails_LabVisits",
                table: "VisitsDetails",
                column: "LabVisitID",
                principalTable: "LabVisits",
                principalColumn: "LabVisitID");
        }
    }
}
