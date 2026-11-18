# College Assignment Portal (.NET 10)

A beginner-friendly ASP.NET Core Minimal API + HTML portal using PostgreSQL and ADO.NET.

## Included
- Login for Admin, Staff and Student demo accounts
- Dashboard counts
- Staff/Admin can create assignments
- Students can view assignments and upload PDF/DOC/DOCX files (10 MB max)
- Students can view their submissions
- Staff/Admin can view all submissions and download uploaded files

## Requirements
- .NET 10 SDK
- PostgreSQL
- Visual Studio 2022 with ASP.NET workload, or VS Code

## Run locally
Create a PostgreSQL database, set its connection string in the environment, and run:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=CollegeAssignmentPortal;Username=postgres;Password=YOUR_PASSWORD"
dotnet run
```

The app creates the schema and demo users from `database.sql` at startup.

## Deploy to Render
1. Push this repository to GitHub.
2. In Render, open the existing `college-assignment-portal` Blueprint and sync it with `render.yaml`.
3. Review the free web service configuration. The app uses the existing `connect-db` PostgreSQL database in Singapore and stores its tables in an isolated `college_assignment` schema.
4. Open the deployed service URL after the first successful deploy.

The free web service sleeps after inactivity; uploaded files are temporary and can disappear on restarts or redeploys. The reused free PostgreSQL database expires after 30 days. Render requires paid resources for persistent service disks.

## Demo logins
- Admin: `admin@college.com` / `Admin@123`
- Staff: `staff@college.com` / `Staff@123`
- Student: `student@college.com` / `Student@123`

## Important learning/demo limitations
This is an academic demo, not production-ready authentication. Passwords are plain text and the demo accounts are seeded automatically. Before production use, replace this with ASP.NET Core Identity or secure password hashing, add CSRF protection, validate authorization at the record level, and use private, persistent file storage with malware scanning.

The Staff submissions page is `/staff/submissions`.


## Updates
- Fixed login to retrieve the UserId required by the session.
- Added the Staff/Admin submissions page to navigation and dashboard.
- Added the explicit System.Net import for HTML encoding.
- Migrated SQL Server data access and schema to PostgreSQL for Render.
- Added a Dockerfile and Render Blueprint for a free demo deployment.
