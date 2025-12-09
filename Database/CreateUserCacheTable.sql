-- ============================================================================
-- User Cache Table for Hybrid Authentication
-- Purpose: Cache AD user credentials and info for offline authentication
-- ============================================================================

-- SQL Server Version
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User_Cache')
BEGIN
    CREATE TABLE [dbo].[User_Cache] (
        [Id]                INT IDENTITY(1,1) PRIMARY KEY,
        [Username]          NVARCHAR(100) NOT NULL UNIQUE,
        [PasswordHash]      NVARCHAR(256) NOT NULL,
        [Salt]              NVARCHAR(64) NOT NULL,

        -- User Info from AD
        [DisplayName]       NVARCHAR(200) NULL,
        [Email]             NVARCHAR(200) NULL,
        [Department]        NVARCHAR(100) NULL,
        [Title]             NVARCHAR(100) NULL,
        [EmployeeId]        NVARCHAR(50) NULL,
        [Groups]            NVARCHAR(MAX) NULL,  -- JSON array of group names

        -- Status & Timestamps
        [IsEnabled]         BIT NOT NULL DEFAULT 1,
        [IsLocked]          BIT NOT NULL DEFAULT 0,
        [FailedAttempts]    INT NOT NULL DEFAULT 0,
        [LastFailedLogin]   DATETIME2 NULL,
        [LastLogin]         DATETIME2 NULL,
        [LastAdSync]        DATETIME2 NOT NULL,  -- Last successful AD verification
        [CacheExpires]      DATETIME2 NOT NULL,  -- When cache expires (90 days default)

        -- Audit
        [CreatedDateTime]   DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDateTime]   DATETIME2 NOT NULL DEFAULT GETDATE(),

        -- Indexes
        INDEX [IX_User_Cache_Username] NONCLUSTERED ([Username]),
        INDEX [IX_User_Cache_CacheExpires] NONCLUSTERED ([CacheExpires]),
        INDEX [IX_User_Cache_Email] NONCLUSTERED ([Email])
    );

    PRINT 'User_Cache table created successfully (SQL Server)';
END
ELSE
BEGIN
    PRINT 'User_Cache table already exists (SQL Server)';
END
GO

-- ============================================================================
-- PostgreSQL Version (Alternative)
-- ============================================================================
/*
CREATE TABLE IF NOT EXISTS user_cache (
    id                  SERIAL PRIMARY KEY,
    username            VARCHAR(100) NOT NULL UNIQUE,
    password_hash       VARCHAR(256) NOT NULL,
    salt                VARCHAR(64) NOT NULL,

    -- User Info from AD
    display_name        VARCHAR(200),
    email               VARCHAR(200),
    department          VARCHAR(100),
    title               VARCHAR(100),
    employee_id         VARCHAR(50),
    groups              TEXT,  -- JSON array

    -- Status & Timestamps
    is_enabled          BOOLEAN NOT NULL DEFAULT TRUE,
    is_locked           BOOLEAN NOT NULL DEFAULT FALSE,
    failed_attempts     INTEGER NOT NULL DEFAULT 0,
    last_failed_login   TIMESTAMP,
    last_login          TIMESTAMP,
    last_ad_sync        TIMESTAMP NOT NULL,
    cache_expires       TIMESTAMP NOT NULL,

    -- Audit
    created_datetime    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_datetime    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_user_cache_username ON user_cache(username);
CREATE INDEX IF NOT EXISTS idx_user_cache_cache_expires ON user_cache(cache_expires);
CREATE INDEX IF NOT EXISTS idx_user_cache_email ON user_cache(email);
*/

-- ============================================================================
-- Login Audit Log Table
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Login_Audit')
BEGIN
    CREATE TABLE [dbo].[Login_Audit] (
        [LogId]             BIGINT IDENTITY(1,1) PRIMARY KEY,
        [Username]          NVARCHAR(100) NOT NULL,
        [LoginTime]         DATETIME2 NOT NULL DEFAULT GETDATE(),
        [Success]           BIT NOT NULL,
        [AuthSource]        NVARCHAR(20) NULL,  -- 'api', 'cache', 'error'
        [FailureReason]     NVARCHAR(500) NULL,
        [IpAddress]         NVARCHAR(50) NULL,
        [UserAgent]         NVARCHAR(500) NULL,
        [SessionId]         NVARCHAR(100) NULL,

        INDEX [IX_Login_Audit_Username] NONCLUSTERED ([Username]),
        INDEX [IX_Login_Audit_LoginTime] NONCLUSTERED ([LoginTime]),
        INDEX [IX_Login_Audit_Success] NONCLUSTERED ([Success])
    );

    PRINT 'Login_Audit table created successfully';
END
ELSE
BEGIN
    PRINT 'Login_Audit table already exists';
END
GO

-- ============================================================================
-- Stored Procedures
-- ============================================================================

-- Procedure to get user by username
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetUserCache')
    DROP PROCEDURE sp_GetUserCache;
GO

CREATE PROCEDURE sp_GetUserCache
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Username,
        PasswordHash,
        Salt,
        DisplayName,
        Email,
        Department,
        Title,
        EmployeeId,
        Groups,
        IsEnabled,
        IsLocked,
        FailedAttempts,
        LastFailedLogin,
        LastLogin,
        LastAdSync,
        CacheExpires,
        CreatedDateTime,
        UpdatedDateTime
    FROM User_Cache
    WHERE Username = @Username;
END
GO

-- Procedure to upsert user cache
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_UpsertUserCache')
    DROP PROCEDURE sp_UpsertUserCache;
GO

CREATE PROCEDURE sp_UpsertUserCache
    @Username NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @Salt NVARCHAR(64),
    @DisplayName NVARCHAR(200) = NULL,
    @Email NVARCHAR(200) = NULL,
    @Department NVARCHAR(100) = NULL,
    @Title NVARCHAR(100) = NULL,
    @EmployeeId NVARCHAR(50) = NULL,
    @Groups NVARCHAR(MAX) = NULL,
    @IsEnabled BIT = 1,
    @CacheDurationDays INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CacheExpires DATETIME2 = DATEADD(DAY, @CacheDurationDays, GETDATE());

    IF EXISTS (SELECT 1 FROM User_Cache WHERE Username = @Username)
    BEGIN
        -- Update existing
        UPDATE User_Cache
        SET
            PasswordHash = @PasswordHash,
            Salt = @Salt,
            DisplayName = @DisplayName,
            Email = @Email,
            Department = @Department,
            Title = @Title,
            EmployeeId = @EmployeeId,
            Groups = @Groups,
            IsEnabled = @IsEnabled,
            IsLocked = 0,
            FailedAttempts = 0,
            LastLogin = GETDATE(),
            LastAdSync = GETDATE(),
            CacheExpires = @CacheExpires,
            UpdatedDateTime = GETDATE()
        WHERE Username = @Username;
    END
    ELSE
    BEGIN
        -- Insert new
        INSERT INTO User_Cache (
            Username, PasswordHash, Salt,
            DisplayName, Email, Department, Title, EmployeeId, Groups,
            IsEnabled, LastLogin, LastAdSync, CacheExpires
        )
        VALUES (
            @Username, @PasswordHash, @Salt,
            @DisplayName, @Email, @Department, @Title, @EmployeeId, @Groups,
            @IsEnabled, GETDATE(), GETDATE(), @CacheExpires
        );
    END
END
GO

-- Procedure to update last login
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_UpdateLastLogin')
    DROP PROCEDURE sp_UpdateLastLogin;
GO

CREATE PROCEDURE sp_UpdateLastLogin
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE User_Cache
    SET
        LastLogin = GETDATE(),
        UpdatedDateTime = GETDATE()
    WHERE Username = @Username;
END
GO

-- Procedure to record failed login attempt
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_RecordFailedLogin')
    DROP PROCEDURE sp_RecordFailedLogin;
GO

CREATE PROCEDURE sp_RecordFailedLogin
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE User_Cache
    SET
        FailedAttempts = FailedAttempts + 1,
        LastFailedLogin = GETDATE(),
        IsLocked = CASE WHEN FailedAttempts >= 4 THEN 1 ELSE 0 END,  -- Lock after 5 attempts
        UpdatedDateTime = GETDATE()
    WHERE Username = @Username;

    -- If user doesn't exist in cache, we still want to log the attempt
    IF @@ROWCOUNT = 0
    BEGIN
        -- Could insert a record or just ignore for non-existent users
        PRINT 'Failed login for non-cached user: ' + @Username;
    END
END
GO

-- Procedure to reset failed attempts
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_ResetFailedAttempts')
    DROP PROCEDURE sp_ResetFailedAttempts;
GO

CREATE PROCEDURE sp_ResetFailedAttempts
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE User_Cache
    SET
        FailedAttempts = 0,
        IsLocked = 0,
        UpdatedDateTime = GETDATE()
    WHERE Username = @Username;
END
GO

-- Procedure to log login audit
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_LogLoginAttempt')
    DROP PROCEDURE sp_LogLoginAttempt;
GO

CREATE PROCEDURE sp_LogLoginAttempt
    @Username NVARCHAR(100),
    @Success BIT,
    @AuthSource NVARCHAR(20) = NULL,
    @FailureReason NVARCHAR(500) = NULL,
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @SessionId NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Login_Audit (
        Username, Success, AuthSource, FailureReason,
        IpAddress, UserAgent, SessionId
    )
    VALUES (
        @Username, @Success, @AuthSource, @FailureReason,
        @IpAddress, @UserAgent, @SessionId
    );
END
GO

-- Procedure to cleanup expired cache entries
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CleanupExpiredCache')
    DROP PROCEDURE sp_CleanupExpiredCache;
GO

CREATE PROCEDURE sp_CleanupExpiredCache
    @DaysToKeepExpired INT = 30  -- Keep expired entries for 30 days before deletion
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeepExpired, GETDATE());

    DELETE FROM User_Cache
    WHERE CacheExpires < @CutoffDate;

    SELECT @@ROWCOUNT AS DeletedCount;
END
GO

PRINT 'All stored procedures created successfully';
PRINT '';
PRINT '=============================================================';
PRINT 'Database setup complete!';
PRINT '';
PRINT 'Tables created:';
PRINT '  - User_Cache: Stores cached user credentials and info';
PRINT '  - Login_Audit: Tracks all login attempts';
PRINT '';
PRINT 'Stored Procedures:';
PRINT '  - sp_GetUserCache';
PRINT '  - sp_UpsertUserCache';
PRINT '  - sp_UpdateLastLogin';
PRINT '  - sp_RecordFailedLogin';
PRINT '  - sp_ResetFailedAttempts';
PRINT '  - sp_LogLoginAttempt';
PRINT '  - sp_CleanupExpiredCache';
PRINT '=============================================================';
