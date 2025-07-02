-- สคริปต์สำหรับสร้างตารางประวัติการใช้งาน Oracle Database
-- รันในฐานข้อมูล HRM_IPS (SQL Server)

-- 1. ตารางหลักสำหรับเก็บประวัติการทำงานกับ Oracle
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OracleActivityHistory')
BEGIN
    CREATE TABLE OracleActivityHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SessionId NVARCHAR(50) NOT NULL,
        UserId NVARCHAR(100) NULL,
        UserName NVARCHAR(200) NULL,
        ActivityType NVARCHAR(50) NOT NULL,
        SqlCommand NVARCHAR(MAX) NULL,
        Parameters NVARCHAR(MAX) NULL,
        TableName NVARCHAR(200) NULL,
        AffectedRows INT DEFAULT 0,
        ExecutionTimeMs INT DEFAULT 0,
        IsSuccess BIT DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        ClientIP NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        CreatedDate DATETIME2 DEFAULT GETDATE()
    );
    PRINT 'ตาราง OracleActivityHistory ถูกสร้างเรียบร้อยแล้ว';
END
ELSE
BEGIN
    PRINT 'ตาราง OracleActivityHistory มีอยู่แล้ว';
END

-- 2. ตารางสำหรับเก็บรายละเอียดการเปลี่ยนแปลงข้อมูล
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OracleDataChangeHistory')
BEGIN
    CREATE TABLE OracleDataChangeHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ActivityHistoryId BIGINT NOT NULL,
        TableName NVARCHAR(200) NOT NULL,
        ColumnName NVARCHAR(200) NOT NULL,
        RecordIdentifier NVARCHAR(500) NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        DataType NVARCHAR(50) NULL,
        CreatedDate DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (ActivityHistoryId) REFERENCES OracleActivityHistory(Id) ON DELETE CASCADE
    );
    PRINT 'ตาราง OracleDataChangeHistory ถูกสร้างเรียบร้อยแล้ว';
END
ELSE
BEGIN
    PRINT 'ตาราง OracleDataChangeHistory มีอยู่แล้ว';
END

-- 3. ตารางสำหรับเก็บข้อมูล Performance ของ Query
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OracleQueryPerformance')
BEGIN
    CREATE TABLE OracleQueryPerformance (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ActivityHistoryId BIGINT NOT NULL,
        SqlHash NVARCHAR(64) NULL,
        QueryType NVARCHAR(50) NULL,
        RowsReturned INT DEFAULT 0,
        ExecutionTimeMs INT DEFAULT 0,
        MemoryUsedKB INT DEFAULT 0,
        CacheHit BIT DEFAULT 0,
        CreatedDate DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (ActivityHistoryId) REFERENCES OracleActivityHistory(Id) ON DELETE CASCADE
    );
    PRINT 'ตาราง OracleQueryPerformance ถูกสร้างเรียบร้อยแล้ว';
END
ELSE
BEGIN
    PRINT 'ตาราง OracleQueryPerformance มีอยู่แล้ว';
END

-- 4. ตารางสำหรับเก็บข้อมูล Session ของผู้ใช้
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OracleUserSessions')
BEGIN
    CREATE TABLE OracleUserSessions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SessionId NVARCHAR(50) NOT NULL UNIQUE,
        UserId NVARCHAR(100) NULL,
        UserName NVARCHAR(200) NULL,
        LoginTime DATETIME2 DEFAULT GETDATE(),
        LastActivity DATETIME2 DEFAULT GETDATE(),
        LogoutTime DATETIME2 NULL,
        IsActive BIT DEFAULT 1,
        ClientIP NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        TotalQueries INT DEFAULT 0,
        TotalUpdates INT DEFAULT 0,
        TotalDeletes INT DEFAULT 0
    );
    PRINT 'ตาราง OracleUserSessions ถูกสร้างเรียบร้อยแล้ว';
END
ELSE
BEGIN
    PRINT 'ตาราง OracleUserSessions มีอยู่แล้ว';
END

-- 5. ตารางสำหรับเก็บการตั้งค่าระบบ
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OracleSystemSettings')
BEGIN
    CREATE TABLE OracleSystemSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SettingKey NVARCHAR(100) UNIQUE NOT NULL,
        SettingValue NVARCHAR(MAX) NULL,
        SettingType NVARCHAR(50) DEFAULT 'String',
        Description NVARCHAR(500) NULL,
        IsSystem BIT DEFAULT 0,
        CreatedBy NVARCHAR(100) NULL,
        CreatedDate DATETIME2 DEFAULT GETDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedDate DATETIME2 DEFAULT GETDATE()
    );
    PRINT 'ตาราง OracleSystemSettings ถูกสร้างเรียบร้อยแล้ว';
END
ELSE
BEGIN
    PRINT 'ตาราง OracleSystemSettings มีอยู่แล้ว';
END

-- สร้าง Index สำหรับประสิทธิภาพ
CREATE NONCLUSTERED INDEX IX_OracleActivityHistory_SessionId 
ON OracleActivityHistory (SessionId) 
WHERE SessionId IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_OracleActivityHistory_ActivityType 
ON OracleActivityHistory (ActivityType) 
WHERE ActivityType IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_OracleActivityHistory_CreatedDate 
ON OracleActivityHistory (CreatedDate DESC);

CREATE NONCLUSTERED INDEX IX_OracleActivityHistory_SessionId_UserId 
ON OracleActivityHistory (SessionId, UserId) 
WHERE SessionId IS NOT NULL;

-- เพิ่มข้อมูลการตั้งค่าเริ่มต้น
IF NOT EXISTS (SELECT * FROM OracleSystemSettings WHERE SettingKey = 'MaxQueryExecutionTimeMs')
BEGIN
    INSERT INTO OracleSystemSettings (SettingKey, SettingValue, SettingType, Description, IsSystem, CreatedBy, UpdatedBy)
    VALUES ('MaxQueryExecutionTimeMs', '30000', 'Integer', 'เวลาสูงสุดในการทำงานของ Query (milliseconds)', 1, 'System', 'System');
END

IF NOT EXISTS (SELECT * FROM OracleSystemSettings WHERE SettingKey = 'MaxRowsPerQuery')
BEGIN
    INSERT INTO OracleSystemSettings (SettingKey, SettingValue, SettingType, Description, IsSystem, CreatedBy, UpdatedBy)
    VALUES ('MaxRowsPerQuery', '10000', 'Integer', 'จำนวนแถวสูงสุดที่จะแสดงผลใน Query', 1, 'System', 'System');
END

IF NOT EXISTS (SELECT * FROM OracleSystemSettings WHERE SettingKey = 'LogRetentionDays')
BEGIN
    INSERT INTO OracleSystemSettings (SettingKey, SettingValue, SettingType, Description, IsSystem, CreatedBy, UpdatedBy)
    VALUES ('LogRetentionDays', '30', 'Integer', 'จำนวนวันที่เก็บประวัติการใช้งาน', 1, 'System', 'System');
END

IF NOT EXISTS (SELECT * FROM OracleSystemSettings WHERE SettingKey = 'EnableQueryLogging')
BEGIN
    INSERT INTO OracleSystemSettings (SettingKey, SettingValue, SettingType, Description, IsSystem, CreatedBy, UpdatedBy)
    VALUES ('EnableQueryLogging', 'true', 'Boolean', 'เปิดใช้งานการบันทึกประวัติ Query', 1, 'System', 'System');
END

-- เพิ่มข้อมูลทดสอบ (ไม่บังคับ)
DECLARE @SessionId NVARCHAR(50) = NEWID();
DECLARE @UserId NVARCHAR(100) = 'TestUser';
DECLARE @UserName NVARCHAR(200) = 'Test User';

-- เพิ่ม Session ทดสอบ
INSERT INTO OracleUserSessions (SessionId, UserId, UserName, ClientIP, UserAgent)
VALUES (@SessionId, @UserId, @UserName, '127.0.0.1', 'Test Browser');

-- เพิ่มข้อมูลประวัติทดสอบ
INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, TableName, AffectedRows, ExecutionTimeMs, IsSuccess)
VALUES 
    (@SessionId, @UserId, @UserName, 'CONNECTION_TEST', 'SELECT ''Oracle Connection Success!'' FROM DUAL', 'DUAL', 1, 150, 1),
    (@SessionId, @UserId, @UserName, 'SELECT', 'SELECT * FROM TEmpLeave WHERE EmpNo = :EmpNo', 'TEmpLeave', 5, 245, 1),
    (@SessionId, @UserId, @UserName, 'UPDATE', 'UPDATE TEmpLeave SET Status = ''Approved'' WHERE EmpNo = :EmpNo', 'TEmpLeave', 2, 180, 1);

PRINT 'การสร้างตารางและข้อมูลเริ่มต้นเสร็จสมบูรณ์';
PRINT 'รันคำสั่ง SELECT COUNT(*) FROM OracleActivityHistory เพื่อตรวจสอบข้อมูล';

-- ตรวจสอบผลลัพธ์
SELECT 
    'OracleActivityHistory' as TableName,
    COUNT(*) as RecordCount
FROM OracleActivityHistory
UNION ALL
SELECT 
    'OracleUserSessions' as TableName,
    COUNT(*) as RecordCount
FROM OracleUserSessions
UNION ALL
SELECT 
    'OracleSystemSettings' as TableName,
    COUNT(*) as RecordCount
FROM OracleSystemSettings; 