-- ========================================
-- Create Error Logs Table
-- Supports both SQL Server and PostgreSQL
-- ========================================

-- For SQL Server:
-- Run this on your SQL Server database

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ErrorLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ErrorLogs] (
        [LogId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [LogLevel] NVARCHAR(20) NOT NULL,  -- ERROR, WARNING, INFO
        [Message] NVARCHAR(500) NOT NULL,
        [ExceptionType] NVARCHAR(255) NULL,
        [ExceptionMessage] NVARCHAR(MAX) NULL,
        [StackTrace] NVARCHAR(MAX) NULL,
        [InnerException] NVARCHAR(MAX) NULL,
        [AdditionalInfo] NVARCHAR(MAX) NULL,
        [RequestUrl] NVARCHAR(500) NULL,
        [UserIP] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [CreatedDateTime] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ServerName] NVARCHAR(100) NULL,
        [ApplicationName] NVARCHAR(100) NULL
    );

    -- Create indexes for better query performance
    CREATE INDEX IX_ErrorLogs_CreatedDateTime ON [dbo].[ErrorLogs] ([CreatedDateTime] DESC);
    CREATE INDEX IX_ErrorLogs_LogLevel ON [dbo].[ErrorLogs] ([LogLevel]);
    CREATE INDEX IX_ErrorLogs_UserIP ON [dbo].[ErrorLogs] ([UserIP]);

    PRINT 'Table ErrorLogs created successfully with indexes'
END
ELSE
BEGIN
    PRINT 'Table ErrorLogs already exists'
END
GO

-- ========================================
-- For PostgreSQL:
-- Run this on your PostgreSQL database
-- ========================================

/*
-- Uncomment this section if using PostgreSQL

CREATE TABLE IF NOT EXISTS public."ErrorLogs" (
    "LogId" BIGSERIAL PRIMARY KEY,
    "LogLevel" VARCHAR(20) NOT NULL,  -- ERROR, WARNING, INFO
    "Message" VARCHAR(500) NOT NULL,
    "ExceptionType" VARCHAR(255) NULL,
    "ExceptionMessage" TEXT NULL,
    "StackTrace" TEXT NULL,
    "InnerException" TEXT NULL,
    "AdditionalInfo" TEXT NULL,
    "RequestUrl" VARCHAR(500) NULL,
    "UserIP" VARCHAR(50) NULL,
    "UserAgent" VARCHAR(500) NULL,
    "CreatedDateTime" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ServerName" VARCHAR(100) NULL,
    "ApplicationName" VARCHAR(100) NULL
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_errorlogs_createddatetime ON public."ErrorLogs" ("CreatedDateTime" DESC);
CREATE INDEX IF NOT EXISTS idx_errorlogs_loglevel ON public."ErrorLogs" ("LogLevel");
CREATE INDEX IF NOT EXISTS idx_errorlogs_userip ON public."ErrorLogs" ("UserIP");

-- Grant permissions (adjust as needed)
GRANT SELECT, INSERT ON public."ErrorLogs" TO your_application_user;

*/

-- ========================================
-- Sample Queries
-- ========================================

-- View latest 50 errors
-- SELECT TOP 50 * FROM ErrorLogs ORDER BY CreatedDateTime DESC;

-- View errors from last 24 hours
-- SELECT * FROM ErrorLogs WHERE CreatedDateTime > DATEADD(HOUR, -24, GETDATE()) ORDER BY CreatedDateTime DESC;

-- Count errors by type
-- SELECT LogLevel, COUNT(*) as Count FROM ErrorLogs GROUP BY LogLevel ORDER BY Count DESC;

-- Find errors by IP address
-- SELECT * FROM ErrorLogs WHERE UserIP = '203.154.xx.xx' ORDER BY CreatedDateTime DESC;

-- Delete old logs (older than 30 days)
-- DELETE FROM ErrorLogs WHERE CreatedDateTime < DATEADD(DAY, -30, GETDATE());
