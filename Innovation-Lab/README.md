# Innovation Lab Technology Management Portal

A web-based system developed to organize the operations of the Innovation Lab at King Abdulaziz University. The platform supports device and technology management, reservation requests, maintenance tracking, user management, and communication between beneficiaries, supervisors, and administrators.

## Project Context

- **Project Type:** Team Project
- **Program:** Summer Training 2025 – Group 3
- **Institution:** King Abdulaziz University
- **College:** Faculty of Computing and Information Technology
- **Client Context:** Deanship of E-Learning and Distance Education

## System Users

The system serves three main user groups:

- **Administrator:** Manages devices, reservations, users, maintenance, reports, and request decisions.
- **Supervisor:** Reviews requests, manages available technologies, submits maintenance requests, and adds new technologies.
- **Students and Faculty Members:** Browse available technologies, submit reservation requests, track bookings, and receive notifications.

## Main Features

- Role-based login and access
- Device and technology catalog
- Search and filtering by category and availability
- Reservation request submission and tracking
- Request approval and rejection workflow
- Supervisor request-management interface
- Technology and device management
- Maintenance request management
- Notifications
- Reports and statistics
- Frequently Asked Questions page

## My Contribution

### Lama Zaki Alshikh — System Analyst & Developer

My responsibilities in this team project included:

- **Database Design and Development**
  - Designed the database structure and entity relationships.
  - Organized data for users, roles, devices, reservation requests, reservations, acceptance decisions, and maintenance records.
  - Supported the system workflow through structured relational data.

- **Frequently Asked Questions Page**
  - Developed the FAQ page to present common questions and answers in a clear and accessible format.
  - Organized the content to help users understand the platform and its services.

- **Supervisor Interface**
  - Developed the supervisor-facing pages.
  - Supported viewing and searching reservation requests.
  - Added request-details views.
  - Supported technology management, adding new technologies, and submitting maintenance requests.

## Database Overview

The database includes the following main entities:

- User Role
- User
- Devices
- Reservation Request
- Reservation
- Acceptance Request
- Maintenance

These entities support user permissions, device availability, reservation workflows, request decisions, and maintenance tracking.

## Technologies Used

- ASP.NET Core MVC
- .NET 8
- C#
- Razor Views
- Entity Framework Core
- SQL Server
- HTML5
- CSS3
- JavaScript
- Visual Studio

## Project Structure

```text
Innovation-Lab
├── WepApp2.sln
└── WepApp2
    ├── Controllers
    ├── Data
    ├── Migrations
    ├── Models
    ├── Views
    ├── wwwroot
    ├── Program.cs
    └── WepApp2.csproj
```

## Local Setup

1. Open `WepApp2.sln` in Visual Studio.
2. Restore the NuGet packages.
3. Configure the SQL Server connection string in a local configuration file or .NET User Secrets.
4. Apply the database migrations.
5. Run the project.

Example commands:

```bash
dotnet restore
dotnet ef database update --project WepApp2/WepApp2.csproj
dotnet run --project WepApp2/WepApp2.csproj
```

## Security and Privacy

- Do not publish real passwords, API keys, or private connection strings.
- Do not upload personal user information or confidential university data.
- Store sensitive configuration using .NET User Secrets or environment variables.

## Team Project Attribution

This system was developed collaboratively as a team project. The contribution described in the **My Contribution** section represents the work completed by **Lama Zaki Alshikh**.

**Original team repository:**  
[Leena-alnahdi/WepApp2-Merged](https://github.com/Leena-alnahdi/WepApp2-Merged)
