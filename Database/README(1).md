# Database Projects

This repository contains two relational database projects developed using **Microsoft SQL Server** as part of academic and training projects.

The databases demonstrate practical experience in database design, table creation, primary and foreign key relationships, data modeling, data import, and integration with ASP.NET Core MVC applications.

## 1. ACADIFY Academic Advising Database

ACADIFY is an academic advising system designed to support students, academic advisors, and administrators.

The database supports academic processes such as student records, academic plans, course recommendations, transcript analysis, meetings, advising forms, notifications, and graduation evaluation.

### Database Highlights

- 29 relational tables
- 32 foreign-key relationships
- 1 database view
- 1 stored procedure
- More than 1,500 sample records
- Primary and foreign key constraints
- Excel-based academic record import
- Integration with ASP.NET Core MVC
- Storage of AI-generated meeting summaries produced through the OpenAI API

### Main Data Areas

- Students and academic advisors
- Courses and study plans
- Student transcripts
- Course prerequisites
- Academic meetings
- Advising forms
- Notifications
- Graduation requirements
- Course recommendations

## 2. Innovation Lab Management Database

This database was developed for an Innovation Lab management system that digitizes service requests, device reservations, consultations, courses, and approval workflows.

### Database Highlights

- 13 relational tables
- 20 foreign-key relationships
- Structured relationships between users, services, devices, and requests
- Integration with an ASP.NET Core MVC web application
- Support for CRUD operations and approval workflows

### Main Data Areas

- Users
- Devices
- Technologies
- Services
- Device bookings
- Device loans
- Lab visits
- Consultations
- Courses
- Service requests

## Sample Data Disclaimer

All records included in these databases are **fictional and created for educational, testing, and demonstration purposes only**.

The sample data does not represent real students, employees, university staff, customers, or organizations. Any names, email addresses, phone numbers, passwords, identification numbers, academic records, or other personal information are entirely fictional.

No confidential or real personal data is included in this repository.

## Security and Privacy

- Database connection strings and credentials are not included.
- API keys and other sensitive configuration values are excluded.
- OpenAI API credentials must be stored using environment variables or ASP.NET Core User Secrets.
- Sample passwords are fictional and must not be used in a production environment.
- Production systems should store passwords using secure hashing rather than plain text.

## Technologies

- Microsoft SQL Server
- SQL
- T-SQL
- SQL Server Management Studio
- ASP.NET Core MVC
- Entity Framework Core
- Microsoft Excel
- OpenAI API

## How to Use

1. Open Microsoft SQL Server Management Studio.
2. Create a new database for the selected project.
3. Open the provided SQL script.
4. Update the database name in the `USE` statement when necessary.
5. Execute the database schema script.
6. Execute the sample data script if it is provided.
7. Refresh the database to view the tables, relationships, views, and stored procedures.

## Repository Structure

```text
Database-Projects/
│
├── ACADIFY/
│   ├── Database-Schema.sql
│   ├── Sample-Data.sql
│   └── Database-Diagram.png
│
├── Innovation-Lab/
│   ├── Database-Schema.sql
│   ├── Sample-Data.sql
│   └── Database-Diagram.png
│
└── README.md
```

## Author

**Lma Zaki Alshikh**  
Information Systems Graduate  
GitHub: [LamaAlshikh](https://github.com/LamaAlshikh)
