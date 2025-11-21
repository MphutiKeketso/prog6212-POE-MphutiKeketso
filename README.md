Contract Monthly Claim System (CMCS)

This repository contains the source code for the Contract Monthly Claim System (CMCS), an ASP.NET Core MVC web application designed to streamline the submission and approval process for monthly claims by contract lecturers.

Project Overview

The CMCS is a role-based web application that facilitates the following workflows:

Lecturers: Submit monthly claims for hours worked, specifying modules and hourly rates.

Programme Coordinators: Review and approve or reject claims submitted by lecturers for their specific programmes.

Academic Managers: Perform final verification and approval of claims approved by coordinators.

HR/Admin: Manage user data and generate reports for payment processing.

The system aims to improve efficiency, reduce errors through automated calculations and validations, and provide a transparent audit trail for all claim activities.

Features

User Authentication & Authorization: Secure role-based access using ASP.NET Core Identity.

Claim Submission: Intuitive interface for lecturers to create claims with automated total calculations.

Automated Verification: System checks against predefined constraints (e.g., max hours, hourly rates).

Approval Workflow: Multi-tier approval process (Coordinator -> Manager).

Document Management: Support for uploading supporting documents (e.g., timesheets).

Reporting: Generation of reports for claims status and payment processing.

Dashboards: Role-specific dashboards for quick overview of pending tasks and statistics.

Tech Stack

Framework: ASP.NET Core 10.0 (MVC)

Language: C#

Database: SQL Server (via Entity Framework Core)

Frontend: Razor Views, HTML, CSS, JavaScript (jQuery), Bootstrap

Tools: Visual Studio 2026 (Preview/RC)

Prerequisites

To run this project, you will need:

Visual Studio 2026 (or a compatible version supporting .NET 9.0).

.NET 9.0 SDK.

SQL Server (LocalDB or Express).

Getting Started

Clone the repository:

git clone [https://github.com/MphutiKeketso/prog6212-POE-MphutiKeketso.git](https://github.com/MphutiKeketso/prog6212-POE-MphutiKeketso.git)


Open the project:
Navigate to the project folder and open Contract Monthly Claim System.sln in Visual Studio 2026.

Configure Database:

Open appsettings.json and ensure the ConnectionStrings:DefaultConnection points to your local SQL Server instance.

Open the Package Manager Console (View -> Other Windows -> Package Manager Console).

Run the following command to apply migrations and create the database:

Update-Database


Run the Application:

Set Contract Monthly Claim System as the startup project.

Press F5 or click the Run button (IIS Express or http).

Login Credentials (Default Seeding):

The system seeds default users on the first run (in Development mode). Check Data/SeedData.cs for exact credentials, typically:

Lecturer: lecturer@university.edu / Lecturer123!

Coordinator: coordinator@university.edu / Coordinator123!

Manager: manager@university.edu / Manager123!

Structure

Controllers: Handles HTTP requests and application flow.

Models: Domain entities and ViewModels.

Services: Business logic layer (Claim calculations, Workflow rules, File handling).

Data: Database context and configurations.

Views: Razor pages for the user interface.

wwwroot: Static assets (CSS, JS, Images).

Troubleshooting

Database Connection Error: Ensure SQL Server LocalDB is running (sqllocaldb start mssqllocaldb) or update the connection string to point to a valid SQL instance.

Migration Errors: If you encounter errors related to Microsoft.VisualStudio.Web.CodeGeneration.Design, try removing that package from the .csproj file or adding <NoWarn>$(NoWarn);NU1608</NoWarn> to the property group.

License

This project is for educational purposes as part of the PROG6212 module.
