CREATE DATABASE CollegeAssignmentPortal;
GO
USE CollegeAssignmentPortal;
GO
CREATE TABLE Users(
 UserId INT IDENTITY PRIMARY KEY,
 FullName NVARCHAR(120) NOT NULL,
 Email NVARCHAR(150) NOT NULL UNIQUE,
 PasswordHash NVARCHAR(200) NOT NULL,
 Role NVARCHAR(20) NOT NULL CHECK(Role IN ('Admin','Staff','Student')),
 IsActive BIT NOT NULL DEFAULT 1
);
CREATE TABLE Assignments(
 AssignmentId INT IDENTITY PRIMARY KEY,
 Title NVARCHAR(200) NOT NULL,
 Subject NVARCHAR(120) NOT NULL,
 Description NVARCHAR(MAX) NOT NULL,
 DueDate DATE NOT NULL,
 StaffId INT NOT NULL REFERENCES Users(UserId),
 CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
CREATE TABLE Submissions(
 SubmissionId INT IDENTITY PRIMARY KEY,
 AssignmentId INT NOT NULL REFERENCES Assignments(AssignmentId),
 StudentId INT NOT NULL REFERENCES Users(UserId),
 FileName NVARCHAR(255) NOT NULL,
 StoredName NVARCHAR(255) NOT NULL,
 SubmittedAt DATETIME NOT NULL DEFAULT GETDATE(),
 Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted'
);
INSERT INTO Users(FullName,Email,PasswordHash,Role) VALUES
('Portal Admin','admin@college.com','Admin@123','Admin'),
('Demo Staff','staff@college.com','Staff@123','Staff'),
('Demo Student','student@college.com','Student@123','Student');
GO
