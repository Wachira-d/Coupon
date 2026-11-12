-- ============================================================================
-- Settings tables for Admin/Settings.aspx
--   App_Settings : settings edited from the web (override Web.config appSettings)
--   Admin_Users  : accounts allowed to sign in to the admin area
--
-- Run once on database Coupon_Honda. Safe to run again (skips existing objects).
-- Until this script is run, the app keeps using Web.config values and the
-- ReportPassword list as the allowed admin accounts.
-- ============================================================================

-- SQL Server Version
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'App_Settings')
BEGIN
    CREATE TABLE [dbo].[App_Settings] (
        [SettingKey]        NVARCHAR(100) NOT NULL PRIMARY KEY,
        [SettingValue]      NVARCHAR(MAX) NULL,
        [UpdatedBy]         NVARCHAR(200) NULL,
        [UpdatedDateTime]   DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'App_Settings table created';
END
ELSE
    PRINT 'App_Settings table already exists';
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Admin_Users')
BEGIN
    CREATE TABLE [dbo].[Admin_Users] (
        [Id]                INT IDENTITY(1,1) PRIMARY KEY,
        [Username]          NVARCHAR(200) NOT NULL UNIQUE,   -- email or AD account name, stored lower-case
        [DisplayName]       NVARCHAR(200) NULL,
        [Note]              NVARCHAR(500) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [AddedBy]           NVARCHAR(200) NULL,
        [CreatedDateTime]   DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDateTime]   DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Admin_Users table created';
END
ELSE
    PRINT 'Admin_Users table already exists';
GO

-- Seed the allowed admin list from Web.config ReportPassword (only adds missing entries)
INSERT INTO [dbo].[Admin_Users] ([Username], [AddedBy])
SELECT v.Username, 'Web.config (seed)'
FROM (VALUES
    (N'wachira.diloksumpan@ipsos.com'),
    (N'muslin.ridhrod@ipsos.com'),
    (N'waraporn.kunaboot@ipsos.com'),
    (N'ashwin.kwatra@ipsos.com')
) AS v(Username)
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Admin_Users] a WHERE LOWER(a.[Username]) = v.Username);
PRINT 'Admin_Users seeded';
GO

-- ============================================================================
-- PostgreSQL Version (Alternative) - column names are lower-case to match DatabaseHelper
-- ============================================================================
/*
CREATE TABLE IF NOT EXISTS app_settings (
    settingkey          VARCHAR(100) PRIMARY KEY,
    settingvalue        TEXT,
    updatedby           VARCHAR(200),
    updateddatetime     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS admin_users (
    id                  SERIAL PRIMARY KEY,
    username            VARCHAR(200) NOT NULL UNIQUE,
    displayname         VARCHAR(200),
    note                VARCHAR(500),
    isactive            BOOLEAN NOT NULL DEFAULT TRUE,
    addedby             VARCHAR(200),
    createddatetime     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updateddatetime     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO admin_users (username, addedby)
SELECT v.username, 'Web.config (seed)'
FROM (VALUES
    ('wachira.diloksumpan@ipsos.com'),
    ('muslin.ridhrod@ipsos.com'),
    ('waraporn.kunaboot@ipsos.com'),
    ('ashwin.kwatra@ipsos.com')
) AS v(username)
WHERE NOT EXISTS (SELECT 1 FROM admin_users a WHERE LOWER(a.username) = v.username);
*/
