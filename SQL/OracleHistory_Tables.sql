-- ===============================================
-- ตารางสำหรับเก็บ History การทำงานกับ Oracle Database
-- ===============================================

-- 1. ตารางหลักสำหรับเก็บ Oracle Activity History
CREATE TABLE OracleActivityHistory (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SessionId NVARCHAR(50) NOT NULL,                    -- Session ID ของผู้ใช้
    UserId NVARCHAR(100),                               -- รหัสผู้ใช้
    UserName NVARCHAR(200),                             -- ชื่อผู้ใช้
    ActivityType NVARCHAR(50) NOT NULL,                 -- ประเภทกิจกรรม (SELECT, UPDATE, DELETE, CONNECTION_TEST)
    SqlCommand NVARCHAR(MAX),                           -- SQL Command ที่รัน
    Parameters NVARCHAR(MAX),                           -- Parameters ที่ใช้ (JSON format)
    TableName NVARCHAR(200),                            -- ชื่อตารางที่ทำงานด้วย
    AffectedRows INT DEFAULT 0,                         -- จำนวนแถวที่ได้รับผลกระทบ
    ExecutionTimeMs INT DEFAULT 0,                      -- เวลาในการรัน (milliseconds)
    IsSuccess BIT DEFAULT 1,                            -- สำเร็จหรือไม่
    ErrorMessage NVARCHAR(MAX),                         -- ข้อความ error (ถ้ามี)
    ClientIP NVARCHAR(50),                              -- IP Address ของผู้ใช้
    UserAgent NVARCHAR(500),                            -- User Agent
    CreatedDate DATETIME2(7) DEFAULT GETDATE(),         -- วันเวลาที่สร้าง
    
    -- Indexes
    INDEX IX_OracleActivityHistory_SessionId (SessionId),
    INDEX IX_OracleActivityHistory_UserId (UserId),
    INDEX IX_OracleActivityHistory_ActivityType (ActivityType),
    INDEX IX_OracleActivityHistory_TableName (TableName),
    INDEX IX_OracleActivityHistory_CreatedDate (CreatedDate DESC),
    INDEX IX_OracleActivityHistory_IsSuccess (IsSuccess)
);

-- 2. ตารางสำหรับเก็บรายละเอียดการเปลี่ยนแปลงข้อมูล (สำหรับ UPDATE operations)
CREATE TABLE OracleDataChangeHistory (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ActivityHistoryId BIGINT NOT NULL,                  -- Foreign Key ไปยัง OracleActivityHistory
    TableName NVARCHAR(200) NOT NULL,                   -- ชื่อตาราง
    ColumnName NVARCHAR(200) NOT NULL,                  -- ชื่อคอลัมน์
    RecordIdentifier NVARCHAR(500),                     -- ตัวระบุแถว (เช่น WHERE clause)
    OldValue NVARCHAR(MAX),                             -- ค่าเก่า
    NewValue NVARCHAR(MAX),                             -- ค่าใหม่
    DataType NVARCHAR(50),                              -- ชนิดข้อมูล
    CreatedDate DATETIME2(7) DEFAULT GETDATE(),         -- วันเวลาที่สร้าง
    
    -- Foreign Key
    FOREIGN KEY (ActivityHistoryId) REFERENCES OracleActivityHistory(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_OracleDataChangeHistory_ActivityHistoryId (ActivityHistoryId),
    INDEX IX_OracleDataChangeHistory_TableName (TableName),
    INDEX IX_OracleDataChangeHistory_ColumnName (ColumnName),
    INDEX IX_OracleDataChangeHistory_CreatedDate (CreatedDate DESC)
);

-- 3. ตารางสำหรับเก็บ Query Performance
CREATE TABLE OracleQueryPerformance (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ActivityHistoryId BIGINT NOT NULL,                  -- Foreign Key ไปยัง OracleActivityHistory
    SqlHash NVARCHAR(64),                               -- Hash ของ SQL (สำหรับจัดกลุ่ม query ที่เหมือนกัน)
    QueryType NVARCHAR(50),                             -- ประเภท query (SELECT, UPDATE, DELETE, etc.)
    RowsReturned INT DEFAULT 0,                         -- จำนวนแถวที่ return (สำหรับ SELECT)
    ExecutionTimeMs INT NOT NULL,                       -- เวลาในการรัน
    MemoryUsedKB INT DEFAULT 0,                         -- หน่วยความจำที่ใช้
    CacheHit BIT DEFAULT 0,                             -- Cache hit หรือไม่
    CreatedDate DATETIME2(7) DEFAULT GETDATE(),         -- วันเวลาที่สร้าง
    
    -- Foreign Key
    FOREIGN KEY (ActivityHistoryId) REFERENCES OracleActivityHistory(Id) ON DELETE CASCADE,
    
    -- Indexes
    INDEX IX_OracleQueryPerformance_SqlHash (SqlHash),
    INDEX IX_OracleQueryPerformance_QueryType (QueryType),
    INDEX IX_OracleQueryPerformance_ExecutionTimeMs (ExecutionTimeMs DESC),
    INDEX IX_OracleQueryPerformance_CreatedDate (CreatedDate DESC)
);

-- 4. ตารางสำหรับเก็บข้อมูล Login/Session
CREATE TABLE OracleUserSessions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SessionId NVARCHAR(50) NOT NULL UNIQUE,             -- Session ID
    UserId NVARCHAR(100),                               -- รหัสผู้ใช้
    UserName NVARCHAR(200),                             -- ชื่อผู้ใช้
    LoginTime DATETIME2(7) DEFAULT GETDATE(),           -- เวลา login
    LastActivity DATETIME2(7) DEFAULT GETDATE(),        -- กิจกรรมล่าสุด
    LogoutTime DATETIME2(7),                            -- เวลา logout
    IsActive BIT DEFAULT 1,                             -- สถานะ active หรือไม่
    ClientIP NVARCHAR(50),                              -- IP Address
    UserAgent NVARCHAR(500),                            -- User Agent
    TotalQueries INT DEFAULT 0,                         -- จำนวน query ทั้งหมดใน session นี้
    TotalUpdates INT DEFAULT 0,                         -- จำนวน update ทั้งหมด
    TotalDeletes INT DEFAULT 0,                         -- จำนวน delete ทั้งหมด
    
    -- Indexes
    INDEX IX_OracleUserSessions_UserId (UserId),
    INDEX IX_OracleUserSessions_LoginTime (LoginTime DESC),
    INDEX IX_OracleUserSessions_IsActive (IsActive)
);

-- 5. ตารางสำหรับเก็บ System Configuration และ Settings
CREATE TABLE OracleSystemSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,           -- คีย์การตั้งค่า
    SettingValue NVARCHAR(MAX),                         -- ค่าการตั้งค่า
    SettingType NVARCHAR(50) DEFAULT 'String',          -- ประเภทข้อมูล (String, Integer, Boolean, JSON)
    Description NVARCHAR(500),                          -- คำอธิบาย
    IsSystem BIT DEFAULT 0,                             -- เป็นการตั้งค่าระบบหรือไม่
    CreatedBy NVARCHAR(100),                            -- ใครสร้าง
    CreatedDate DATETIME2(7) DEFAULT GETDATE(),         -- วันที่สร้าง
    UpdatedBy NVARCHAR(100),                            -- ใครแก้ไขล่าสุด
    UpdatedDate DATETIME2(7) DEFAULT GETDATE(),         -- วันที่แก้ไขล่าสุด
    
    -- Index
    INDEX IX_OracleSystemSettings_SettingKey (SettingKey)
);

GO

-- ===============================================
-- Views สำหรับรายงาน
-- ===============================================

-- View สำหรับดู Activity Summary
CREATE VIEW vw_OracleActivitySummary AS
SELECT 
    h.Id,
    h.SessionId,
    h.UserId,
    h.UserName,
    h.ActivityType,
    h.TableName,
    h.AffectedRows,
    h.ExecutionTimeMs,
    h.IsSuccess,
    h.CreatedDate,
    s.LoginTime,
    s.ClientIP
FROM OracleActivityHistory h
LEFT JOIN OracleUserSessions s ON h.SessionId = s.SessionId;

GO

-- View สำหรับดู Performance Summary
CREATE VIEW vw_OraclePerformanceSummary AS
SELECT 
    p.SqlHash,
    p.QueryType,
    COUNT(*) as ExecutionCount,
    AVG(p.ExecutionTimeMs) as AvgExecutionTimeMs,
    MIN(p.ExecutionTimeMs) as MinExecutionTimeMs,
    MAX(p.ExecutionTimeMs) as MaxExecutionTimeMs,
    SUM(p.RowsReturned) as TotalRowsReturned,
    AVG(p.RowsReturned) as AvgRowsReturned
FROM OracleQueryPerformance p
GROUP BY p.SqlHash, p.QueryType;

GO

-- View สำหรับดู Daily Activity Report
CREATE VIEW vw_OracleDailyActivity AS
SELECT 
    CAST(h.CreatedDate AS DATE) as ActivityDate,
    h.ActivityType,
    COUNT(*) as ActivityCount,
    COUNT(CASE WHEN h.IsSuccess = 1 THEN 1 END) as SuccessCount,
    COUNT(CASE WHEN h.IsSuccess = 0 THEN 1 END) as ErrorCount,
    AVG(h.ExecutionTimeMs) as AvgExecutionTimeMs,
    SUM(h.AffectedRows) as TotalAffectedRows
FROM OracleActivityHistory h
GROUP BY CAST(h.CreatedDate AS DATE), h.ActivityType;

GO

-- ===============================================
-- Stored Procedures
-- ===============================================

-- Procedure สำหรับ Clean up old data
CREATE PROCEDURE sp_CleanupOracleHistory
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, GETDATE());
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- ลบข้อมูล Performance ที่เก่า
        DELETE FROM OracleQueryPerformance 
        WHERE CreatedDate < @CutoffDate;
        
        -- ลบข้อมูล Change History ที่เก่า
        DELETE FROM OracleDataChangeHistory 
        WHERE CreatedDate < @CutoffDate;
        
        -- ลบข้อมูล Activity ที่เก่า
        DELETE FROM OracleActivityHistory 
        WHERE CreatedDate < @CutoffDate;
        
        -- ลบ Session ที่ไม่ active และเก่า
        DELETE FROM OracleUserSessions 
        WHERE IsActive = 0 AND LoginTime < @CutoffDate;
        
        COMMIT TRANSACTION;
        
        PRINT 'Cleanup completed successfully for data older than ' + CAST(@RetentionDays AS NVARCHAR(10)) + ' days.';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

GO

-- Procedure สำหรับ Insert Activity History
CREATE PROCEDURE sp_InsertOracleActivity
    @SessionId NVARCHAR(50),
    @UserId NVARCHAR(100) = NULL,
    @UserName NVARCHAR(200) = NULL,
    @ActivityType NVARCHAR(50),
    @SqlCommand NVARCHAR(MAX) = NULL,
    @Parameters NVARCHAR(MAX) = NULL,
    @TableName NVARCHAR(200) = NULL,
    @AffectedRows INT = 0,
    @ExecutionTimeMs INT = 0,
    @IsSuccess BIT = 1,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @ClientIP NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @ActivityId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO OracleActivityHistory (
        SessionId, UserId, UserName, ActivityType, SqlCommand, Parameters,
        TableName, AffectedRows, ExecutionTimeMs, IsSuccess, ErrorMessage,
        ClientIP, UserAgent
    )
    VALUES (
        @SessionId, @UserId, @UserName, @ActivityType, @SqlCommand, @Parameters,
        @TableName, @AffectedRows, @ExecutionTimeMs, @IsSuccess, @ErrorMessage,
        @ClientIP, @UserAgent
    );
    
    SET @ActivityId = SCOPE_IDENTITY();
    
    -- อัพเดท Session activity
    UPDATE OracleUserSessions 
    SET LastActivity = GETDATE(),
        TotalQueries = CASE WHEN @ActivityType = 'SELECT' THEN TotalQueries + 1 ELSE TotalQueries END,
        TotalUpdates = CASE WHEN @ActivityType = 'UPDATE' THEN TotalUpdates + 1 ELSE TotalUpdates END,
        TotalDeletes = CASE WHEN @ActivityType = 'DELETE' THEN TotalDeletes + 1 ELSE TotalDeletes END
    WHERE SessionId = @SessionId;
END;

GO

-- ===============================================
-- Insert ข้อมูลเริ่มต้น
-- ===============================================

-- การตั้งค่าเริ่มต้น
INSERT INTO OracleSystemSettings (SettingKey, SettingValue, SettingType, Description, IsSystem) VALUES
('MaxQueryExecutionTimeMs', '30000', 'Integer', 'เวลาสูงสุดในการรัน query (milliseconds)', 1),
('MaxRowsPerQuery', '10000', 'Integer', 'จำนวนแถวสูงสุดที่อนุญาตต่อ query', 1),
('LogRetentionDays', '30', 'Integer', 'จำนวนวันที่เก็บ log', 1),
('EnableQueryLogging', 'true', 'Boolean', 'เปิดการ log query หรือไม่', 1),
('EnablePerformanceLogging', 'true', 'Boolean', 'เปิดการ log performance หรือไม่', 1),
('AllowedTables', '["TEmpLeave","MEmpBasic","MSecCode"]', 'JSON', 'รายการตารางที่อนุญาตให้เข้าถึง', 1);

GO

-- ===============================================
-- Triggers สำหรับ Audit
-- ===============================================

-- Trigger สำหรับ audit การเปลี่ยนแปลง System Settings
CREATE TRIGGER tr_OracleSystemSettings_Audit
ON OracleSystemSettings
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Log การเปลี่ยนแปลง settings
    IF EXISTS(SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO OracleActivityHistory (
            SessionId, ActivityType, SqlCommand, TableName, 
            AffectedRows, IsSuccess, CreatedDate
        )
        SELECT 
            'SYSTEM',
            CASE WHEN EXISTS(SELECT 1 FROM inserted) THEN 'SETTINGS_UPDATE' ELSE 'SETTINGS_DELETE' END,
            'System Settings Changed',
            'OracleSystemSettings',
            @@ROWCOUNT,
                         1,
             GETDATE();
     END
END;

GO 