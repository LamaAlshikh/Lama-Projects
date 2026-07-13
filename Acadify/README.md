# ACADIFY — Smart Academic Advising System

## لتجريب النظام (Demo Access)

> استخدمي بيانات الحسابات التالية لاختبار واجهات النظام حسب الدور.

### Academic Advisor
- **Username:** `lama`
- **Email:** `lm11@kau.edu.sa`
- **Password:** `L1111`

### Administrator
- **Username:** `lama`
- **Email:** `lm11@kau.edu.sa`
- **Password:** `L1111`

### Student
- **Username:** `last`
- **Email:** `last@stu.kau.edu.sa`
- **Password:** `A1212`

> These credentials are demo credentials only. Do not use real user accounts or real student data in a public repository.

## Project Description

**ACADIFY** is a smart academic advising web system that simplifies academic planning and communication between students, academic advisors, and administrators.

Students can upload their academic transcript, track progress in the study plan, receive course recommendations, submit academic forms, request meetings, and view notifications. Academic advisors can review student records, monitor study-plan matching, approve or reject requests, add notes, and check graduation eligibility. Administrators can manage users, courses, study plans, academic calendars, and system data.

## Main Features

- Role-based access for students, advisors, and administrators
- Academic transcript upload and analysis
- Study-plan progress and course matching
- Smart course recommendations
- Academic forms and approval workflow
- Advisor notes, meeting requests, and notifications
- Graduation-project eligibility tracking
- User, course, and study-plan management

## Technologies

- ASP.NET Core MVC (.NET 8)
- C# and Razor Views
- HTML5, CSS3, and JavaScript
- Entity Framework Core
- SQL Server
- OpenAI API integration

## Local Setup

1. Open `Acadify.sln` in Visual Studio.
2. Restore NuGet packages.
3. Configure the SQL Server connection string.
4. Store API keys and email credentials outside GitHub, preferably with .NET User Secrets or environment variables.
5. Apply the database migrations, then run the project.

Example commands:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AcadifyDb" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"
dotnet user-secrets set "Smtp:Username" "YOUR_EMAIL"
dotnet user-secrets set "Smtp:Password" "YOUR_APP_PASSWORD"
dotnet user-secrets set "Smtp:FromEmail" "YOUR_EMAIL"
```

## Security and Privacy

- No real API keys or email passwords are included in this repository.
- Uploaded transcripts and study-plan files are excluded from source control.
- Do not publish real student records or personal information.
