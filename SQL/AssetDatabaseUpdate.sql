-- =============================================
-- สคริปต์ปรับปรุงฐานข้อมูลระบบจัดการทรัพย์สิน IT
-- =============================================

USE [IPS_TH]
GO

-- =============================================
-- 1. ปรับปรุงตาราง Asset
-- =============================================

-- เพิ่มคอลัมน์ใหม่ในตาราง Asset
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Asset' AND COLUMN_NAME = 'WindowsVersion')
BEGIN
    ALTER TABLE Asset ADD WindowsVersion NVARCHAR(50) NULL;
    PRINT 'เพิ่มคอลัมน์ WindowsVersion ในตาราง Asset';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Asset' AND COLUMN_NAME = 'DeletedDate')
BEGIN
    ALTER TABLE Asset ADD DeletedDate DATETIME NULL;
    PRINT 'เพิ่มคอลัมน์ DeletedDate ในตาราง Asset';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Asset' AND COLUMN_NAME = 'DeletedBy')
BEGIN
    ALTER TABLE Asset ADD DeletedBy NVARCHAR(50) NULL;
    PRINT 'เพิ่มคอลัมน์ DeletedBy ในตาราง Asset';
END

-- =============================================
-- 2. สร้างตาราง AssetMacAddress
-- =============================================

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetMacAddress')
BEGIN
    CREATE TABLE AssetMacAddress (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AssetId INT NOT NULL,
        MacAddress NVARCHAR(17) NOT NULL,
        NetworkType NVARCHAR(20) NULL,
        Remarks NVARCHAR(200) NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) NOT NULL,
        
        CONSTRAINT FK_AssetMacAddress_Asset FOREIGN KEY (AssetId) REFERENCES Asset(Id),
        CONSTRAINT CK_AssetMacAddress_MacAddress CHECK (MacAddress LIKE '[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]')
    );
    PRINT 'สร้างตาราง AssetMacAddress';
END

-- =============================================
-- 3. สร้างตาราง AssetHistory
-- =============================================

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetHistory')
BEGIN
    CREATE TABLE AssetHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AssetId INT NOT NULL,
        ChangeType NVARCHAR(20) NOT NULL,
        FieldName NVARCHAR(50) NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        Description NVARCHAR(500) NULL,
        ChangeDate DATETIME NOT NULL DEFAULT GETDATE(),
        ChangedBy NVARCHAR(50) NOT NULL,
        IpAddress NVARCHAR(15) NULL,
        
        CONSTRAINT FK_AssetHistory_Asset FOREIGN KEY (AssetId) REFERENCES Asset(Id)
    );
    PRINT 'สร้างตาราง AssetHistory';
END

-- =============================================
-- 4. สร้าง Indexes
-- =============================================

-- Index สำหรับ AssetMacAddress
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetMacAddress_AssetId')
BEGIN
    CREATE INDEX IX_AssetMacAddress_AssetId ON AssetMacAddress(AssetId);
    PRINT 'สร้าง Index IX_AssetMacAddress_AssetId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetMacAddress_MacAddress')
BEGIN
    CREATE UNIQUE INDEX IX_AssetMacAddress_MacAddress ON AssetMacAddress(MacAddress) WHERE MacAddress IS NOT NULL;
    PRINT 'สร้าง Index IX_AssetMacAddress_MacAddress';
END

-- Index สำหรับ AssetHistory
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetHistory_AssetId')
BEGIN
    CREATE INDEX IX_AssetHistory_AssetId ON AssetHistory(AssetId);
    PRINT 'สร้าง Index IX_AssetHistory_AssetId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetHistory_ChangeDate')
BEGIN
    CREATE INDEX IX_AssetHistory_ChangeDate ON AssetHistory(ChangeDate);
    PRINT 'สร้าง Index IX_AssetHistory_ChangeDate';
END

-- =============================================
-- 5. สร้าง Stored Procedures
-- =============================================

-- Stored Procedure สำหรับบันทึกประวัติการเปลี่ยนแปลง
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_LogAssetHistory')
    DROP PROCEDURE sp_LogAssetHistory
GO

CREATE PROCEDURE sp_LogAssetHistory
    @AssetId INT,
    @ChangeType NVARCHAR(20),
    @FieldName NVARCHAR(50) = NULL,
    @OldValue NVARCHAR(MAX) = NULL,
    @NewValue NVARCHAR(MAX) = NULL,
    @Description NVARCHAR(500) = NULL,
    @ChangedBy NVARCHAR(50),
    @IpAddress NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO AssetHistory (
        AssetId, ChangeType, FieldName, OldValue, NewValue, 
        Description, ChangeDate, ChangedBy, IpAddress
    )
    VALUES (
        @AssetId, @ChangeType, @FieldName, @OldValue, @NewValue,
        @Description, GETDATE(), @ChangedBy, @IpAddress
    );
END
GO

PRINT 'สร้าง Stored Procedure sp_LogAssetHistory';

-- Stored Procedure สำหรับตรวจสอบ Mac Address ซ้ำ
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CheckMacAddressDuplicate')
    DROP PROCEDURE sp_CheckMacAddressDuplicate
GO

CREATE PROCEDURE sp_CheckMacAddressDuplicate
    @MacAddress NVARCHAR(17),
    @AssetId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Count INT;
    
    IF @AssetId IS NULL
        SELECT @Count = COUNT(*) FROM AssetMacAddress WHERE MacAddress = @MacAddress;
    ELSE
        SELECT @Count = COUNT(*) FROM AssetMacAddress WHERE MacAddress = @MacAddress AND AssetId != @AssetId;
    
    SELECT @Count AS DuplicateCount;
END
GO

PRINT 'สร้าง Stored Procedure sp_CheckMacAddressDuplicate';

-- =============================================
-- 6. สร้าง Functions
-- =============================================

-- Function สำหรับตรวจสอบ Mac Address ถูกต้อง
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'fn_IsValidMacAddress')
    DROP FUNCTION fn_IsValidMacAddress
GO

CREATE FUNCTION fn_IsValidMacAddress(@MacAddress NVARCHAR(17))
RETURNS BIT
AS
BEGIN
    DECLARE @IsValid BIT = 0;
    
    IF @MacAddress IS NOT NULL 
       AND LEN(@MacAddress) = 17
       AND @MacAddress LIKE '[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]:[0-9A-Fa-f][0-9A-Fa-f]'
    BEGIN
        SET @IsValid = 1;
    END
    
    RETURN @IsValid;
END
GO

PRINT 'สร้าง Function fn_IsValidMacAddress';

-- Function สำหรับดึงประวัติการเปลี่ยนแปลงของทรัพย์สิน
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'TF' AND name = 'fn_GetAssetHistory')
    DROP FUNCTION fn_GetAssetHistory
GO

CREATE FUNCTION fn_GetAssetHistory(@AssetId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        Id,
        ChangeType,
        FieldName,
        OldValue,
        NewValue,
        Description,
        ChangeDate,
        ChangedBy,
        IpAddress
    FROM AssetHistory
    WHERE AssetId = @AssetId
    ORDER BY ChangeDate DESC
)
GO

PRINT 'สร้าง Function fn_GetAssetHistory';

-- =============================================
-- 7. สร้าง Views
-- =============================================

-- View สำหรับข้อมูลทรัพย์สินพร้อม Mac Address
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AssetWithMacAddresses')
    DROP VIEW vw_AssetWithMacAddresses
GO

CREATE VIEW vw_AssetWithMacAddresses
AS
SELECT 
    a.*,
    STRING_AGG(am.MacAddress, ', ') AS MacAddresses,
    COUNT(am.Id) AS MacAddressCount
FROM Asset a
LEFT JOIN AssetMacAddress am ON a.Id = am.AssetId
WHERE a.IsDeleted = 0
GROUP BY 
    a.Id, a.AssetNo, a.ProductSerial, a.Name, a.Type, a.Status,
    a.CustomerNo, a.CustomerName, a.Department, a.DepartmentName,
    a.Line, a.LineName, a.Owner, a.OwnerName, a.Location,
    a.LocationDetail, a.Model, a.Manufacturer, a.CountryOfOrigin,
    a.WindowsVersion, a.InstallationDate, a.StartDate, a.EndDate,
    a.Remarks, a.CreatedDate, a.CreatedBy, a.UpdatedDate,
    a.UpdatedBy, a.IsDeleted, a.DeletedDate, a.DeletedBy
GO

PRINT 'สร้าง View vw_AssetWithMacAddresses';

-- View สำหรับสถิติทรัพย์สิน
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AssetStatistics')
    DROP VIEW vw_AssetStatistics
GO

CREATE VIEW vw_AssetStatistics
AS
SELECT 
    COUNT(*) AS TotalAssets,
    SUM(CASE WHEN Status = '1' THEN 1 ELSE 0 END) AS ActiveAssets,
    SUM(CASE WHEN Status = '2' THEN 1 ELSE 0 END) AS RepairAssets,
    SUM(CASE WHEN Status = '3' THEN 1 ELSE 0 END) AS DamagedAssets,
    SUM(CASE WHEN Status = '4' THEN 1 ELSE 0 END) AS DisposedAssets,
    SUM(CASE WHEN Status = '5' THEN 1 ELSE 0 END) AS InactiveAssets,
    COUNT(DISTINCT Owner) AS UniqueOwners,
    COUNT(DISTINCT Department) AS UniqueDepartments
FROM Asset
WHERE IsDeleted = 0
GO

PRINT 'สร้าง View vw_AssetStatistics';

-- =============================================
-- 8. สร้าง Triggers
-- =============================================

-- Trigger สำหรับบันทึกประวัติการเปลี่ยนแปลง Asset
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_Asset_History')
    DROP TRIGGER tr_Asset_History
GO

CREATE TRIGGER tr_Asset_History
ON Asset
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AssetId INT, @ChangeType NVARCHAR(20), @ChangedBy NVARCHAR(50);
    
    SELECT @AssetId = Id, @ChangedBy = UpdatedBy FROM inserted;
    SET @ChangeType = 'Update';
    
    -- บันทึกการเปลี่ยนแปลงสำหรับแต่ละฟิลด์
    IF UPDATE(Name)
    BEGIN
        EXEC sp_LogAssetHistory 
            @AssetId = @AssetId,
            @ChangeType = @ChangeType,
            @FieldName = 'Name',
            @OldValue = (SELECT Name FROM deleted),
            @NewValue = (SELECT Name FROM inserted),
            @Description = 'แก้ไขชื่อทรัพย์สิน',
            @ChangedBy = @ChangedBy;
    END
    
    IF UPDATE(Status)
    BEGIN
        EXEC sp_LogAssetHistory 
            @AssetId = @AssetId,
            @ChangeType = @ChangeType,
            @FieldName = 'Status',
            @OldValue = (SELECT Status FROM deleted),
            @NewValue = (SELECT Status FROM inserted),
            @Description = 'แก้ไขสถานะทรัพย์สิน',
            @ChangedBy = @ChangedBy;
    END
    
    IF UPDATE(Owner)
    BEGIN
        EXEC sp_LogAssetHistory 
            @AssetId = @AssetId,
            @ChangeType = @ChangeType,
            @FieldName = 'Owner',
            @OldValue = (SELECT Owner FROM deleted),
            @NewValue = (SELECT Owner FROM inserted),
            @Description = 'เปลี่ยนผู้รับผิดชอบ',
            @ChangedBy = @ChangedBy;
    END
    
    IF UPDATE(Location)
    BEGIN
        EXEC sp_LogAssetHistory 
            @AssetId = @AssetId,
            @ChangeType = @ChangeType,
            @FieldName = 'Location',
            @OldValue = (SELECT Location FROM deleted),
            @NewValue = (SELECT Location FROM inserted),
            @Description = 'เปลี่ยนสถานที่จัดเก็บ',
            @ChangedBy = @ChangedBy;
    END
END
GO

PRINT 'สร้าง Trigger tr_Asset_History';

-- =============================================
-- 9. ข้อมูลเริ่มต้น (ถ้าจำเป็น)
-- =============================================

-- เพิ่มข้อมูลประเภททรัพย์สิน (ถ้ายังไม่มี)
IF NOT EXISTS (SELECT * FROM Asset WHERE Type = '1')
BEGIN
    -- ตัวอย่างข้อมูลเริ่มต้น
    INSERT INTO Asset (AssetNo, ProductSerial, Name, Type, Status, Owner, OwnerName, Department, DepartmentName, Location, CreatedBy)
    VALUES 
    ('IT001', 'SN001', 'คอมพิวเตอร์สำนักงาน', '1', '1', 'EMP001', 'ทดสอบ ระบบ', 'IT', 'ไอที', 'POR', 'System'),
    ('IT002', 'SN002', 'เครื่องพิมพ์ HP', '5', '1', 'EMP002', 'ทดสอบ ระบบ', 'IT', 'ไอที', 'POR', 'System');
    
    PRINT 'เพิ่มข้อมูลตัวอย่างทรัพย์สิน';
END

-- =============================================
-- 10. สร้าง Indexes เพิ่มเติมสำหรับประสิทธิภาพ
-- =============================================

-- Index สำหรับการค้นหา
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Asset_Search')
BEGIN
    CREATE INDEX IX_Asset_Search ON Asset(Name, AssetNo, ProductSerial, Owner) WHERE IsDeleted = 0;
    PRINT 'สร้าง Index IX_Asset_Search';
END

-- Index สำหรับการเรียงลำดับ
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Asset_CreatedDate')
BEGIN
    CREATE INDEX IX_Asset_CreatedDate ON Asset(CreatedDate DESC) WHERE IsDeleted = 0;
    PRINT 'สร้าง Index IX_Asset_CreatedDate';
END

PRINT 'เสร็จสิ้นการปรับปรุงฐานข้อมูลระบบจัดการทรัพย์สิน IT';
GO 