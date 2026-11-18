# College Assignment Portal (.NET 10)

A beginner-friendly ASP.NET Core Minimal API + HTML portal using SQL Server and ADO.NET.

## Included
- Login for Admin, Staff and Student demo accounts
- Dashboard counts
- Staff/Admin can create assignments
- Students can view assignments and upload PDF/DOC/DOCX files (10 MB max)
- Students can view their submissions
- Staff/Admin can view all submissions and download uploaded files

## Requirements
- .NET 10 SDK
- SQL Server LocalDB (or SQL Server)
- Visual Studio 2022 with ASP.NET workload, or VS Code

## 1. Create database
Open SQL Server Management Studio (SSMS), connect to `(localdb)\\MSSQLLocalDB`, open `database.sql`, and execute it.

If using another SQL Server instance, change `DefaultConnection` in `appsettings.json`.

## 2. Run
Open PowerShell in this folder:

```powershell
dotnet restore
dotnet run
```

Open the localhost URL printed in the terminal.

## Demo logins
- Admin: `admin@college.com` / `Admin@123`
- Staff: `staff@college.com` / `Staff@123`
- Student: `student@college.com` / `Student@123`

## Important learning/demo limitations
This is an academic starter project, not production-ready authentication. Passwords are plain text only to keep the first version easy to understand. Before real deployment, replace this with ASP.NET Core Identity or a secure password-hashing implementation, add CSRF protection, validate authorization at the record level, and configure upload malware scanning and private file storage.

The Staff submissions page is `/staff/submissions`.


## Updates
- Fixed login to retrieve the UserId required by the session.
- Added the Staff/Admin submissions page to navigation and dashboard.
- Added the explicit System.Net import for HTML encoding.
