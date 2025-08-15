-- =============================================
-- สคริปต์ปรับปรุงตาราง Asset และสร้างตารางใหม่
-- =============================================

-- 1. เพิ่มคอลัมน์ WindowsVersion ในตาราง Asset
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Asset' AND COLUMN_NAME = 'WindowsVersion')
BEGIN
    ALTER TABLE Asset ADD WindowsVersion NVARCHAR(50) NULL;
    PRINT 'เพิ่มคอลัมน์ WindowsVersion ในตาราง Asset สำเร็จ';
END

-- 2. สร้างตาราง AssetMacAddress
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetMacAddress')
BEGIN
    CREATE TABLE AssetMacAddress (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AssetId INT NOT NULL,
        MacAddress NVARCHAR(17) NOT NULL,
        NetworkType NVARCHAR(20) NULL,
        Remarks NVARCHAR(200) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) NULL,
        CONSTRAINT FK_AssetMacAddress_Asset FOREIGN KEY (AssetId) REFERENCES Asset(Id)
    );
    PRINT 'สร้างตาราง AssetMacAddress สำเร็จ';
END

-- 3. สร้างตาราง AssetHistory
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
        ChangeDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        ChangedBy NVARCHAR(50) NULL,
        IpAddress NVARCHAR(15) NULL,
        CONSTRAINT FK_AssetHistory_Asset FOREIGN KEY (AssetId) REFERENCES Asset(Id)
    );
    PRINT 'สร้างตาราง AssetHistory สำเร็จ';
END

-- 4. สร้าง Index สำหรับการค้นหา
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetMacAddress_AssetId')
BEGIN
    CREATE INDEX IX_AssetMacAddress_AssetId ON AssetMacAddress(AssetId);
    PRINT 'สร้าง Index IX_AssetMacAddress_AssetId สำเร็จ';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetHistory_AssetId')
BEGIN
    CREATE INDEX IX_AssetHistory_AssetId ON AssetHistory(AssetId);
    PRINT 'สร้าง Index IX_AssetHistory_AssetId สำเร็จ';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssetHistory_ChangeDate')
BEGIN
    CREATE INDEX IX_AssetHistory_ChangeDate ON AssetHistory(ChangeDate);
    PRINT 'สร้าง Index IX_AssetHistory_ChangeDate สำเร็จ';
END

-- 5. สร้าง Stored Procedure สำหรับบันทึกประวัติ
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_LogAssetHistory')
BEGIN
    EXEC('
    CREATE PROCEDURE sp_LogAssetHistory
        @AssetId INT,
        @ChangeType NVARCHAR(20),
        @FieldName NVARCHAR(50) = NULL,
        @OldValue NVARCHAR(MAX) = NULL,
        @NewValue NVARCHAR(MAX) = NULL,
        @Description NVARCHAR(500) = NULL,
        @ChangedBy NVARCHAR(50) = NULL,
        @IpAddress NVARCHAR(15) = NULL
    AS
    BEGIN
        INSERT INTO AssetHistory (AssetId, ChangeType, FieldName, OldValue, NewValue, Description, ChangedBy, IpAddress)
        VALUES (@AssetId, @ChangeType, @FieldName, @OldValue, @NewValue, @Description, @ChangedBy, @IpAddress);
    END
    ');
    PRINT 'สร้าง Stored Procedure sp_LogAssetHistory สำเร็จ';
END

-- 6. สร้าง View สำหรับดูข้อมูล Asset พร้อม Mac Address
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AssetWithMacAddress')
BEGIN
    EXEC('
    CREATE VIEW vw_AssetWithMacAddress AS
    SELECT 
        a.*,
        STRING_AGG(am.MacAddress, '','') AS MacAddresses,
        STRING_AGG(am.NetworkType, '','') AS NetworkTypes
    FROM Asset a
    LEFT JOIN AssetMacAddress am ON a.Id = am.AssetId
    WHERE a.IsDeleted = 0
    GROUP BY 
        a.Id, a.AssetNo, a.ProductSerial, a.Name, a.Type, a.Status,
        a.CustomerNo, a.CustomerName, a.Department, a.DepartmentName,
        a.Line, a.LineName, a.Owner, a.OwnerName, a.Location, a.LocationDetail,
        a.Model, a.Manufacturer, a.CountryOfOrigin, a.WindowsVersion,
        a.InstallationDate, a.StartDate, a.EndDate, a.Remarks,
        a.CreatedDate, a.CreatedBy, a.UpdatedDate, a.UpdatedBy,
        a.IsDeleted, a.DeletedDate, a.DeletedBy
    ');
    PRINT 'สร้าง View vw_AssetWithMacAddress สำเร็จ';
END

-- 7. สร้าง Function สำหรับตรวจสอบ Mac Address ซ้ำ
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'fn_CheckMacAddressExists')
BEGIN
    EXEC('
    CREATE FUNCTION fn_CheckMacAddressExists(@MacAddress NVARCHAR(17), @ExcludeAssetId INT = NULL)
    RETURNS BIT
    AS
    BEGIN
        DECLARE @Exists BIT = 0;
        
        IF EXISTS (
            SELECT 1 FROM AssetMacAddress 
            WHERE MacAddress = @MacAddress 
            AND (@ExcludeAssetId IS NULL OR AssetId != @ExcludeAssetId)
        )
        BEGIN
            SET @Exists = 1;
        END
        
        RETURN @Exists;
    END
    ');
    PRINT 'สร้าง Function fn_CheckMacAddressExists สำเร็จ';
END

PRINT 'เสร็จสิ้นการปรับปรุงฐานข้อมูล'; 