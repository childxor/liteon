using System.Data;
using System.Diagnostics;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.History;
using IPS_TH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace IPS_TH.Controllers
{
    public class OracleTestController : Controller
    {
        private readonly OracleDbContext _oracleContext;
        private readonly IConfiguration _configuration;
        private readonly IOracleHistoryService _historyService;

        public OracleTestController(
            OracleDbContext oracleContext,
            IConfiguration configuration,
            IOracleHistoryService historyService
        )
        {
            _oracleContext = oracleContext;
            _configuration = configuration;
            _historyService = historyService;
        }

        // หน้าสำหรับทดสอบการเชื่อมต่อ Oracle
        public IActionResult Index()
        {
            return View();
        }

        // ตรวจสอบและสร้างตารางประวัติ
        [HttpPost]
        public async Task<IActionResult> InitializeHistoryTables()
        {
            try
            {
                // เชื่อมต่อฐานข้อมูล HRM_IPS
                using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")
                );
                await msConnection.OpenAsync();

                // ตรวจสอบว่าตาราง OracleActivityHistory มีอยู่หรือไม่
                var tableExists = await msConnection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OracleActivityHistory'"
                );

                if (tableExists == 0)
                {
                    // สร้างตารางอัตโนมัติ
                    var createTableSql = @"
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
                        );";

                    await msConnection.ExecuteAsync(createTableSql);

                    // เพิ่มข้อมูลทดสอบ
                    await AddTestData(msConnection);

                    return Json(new { success = true, message = "สร้างตารางและข้อมูลทดสอบเรียบร้อยแล้ว" });
                }
                else
                {
                    // เพิ่มข้อมูลทดสอบถ้าไม่มี
                    var recordCount = await msConnection.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM OracleActivityHistory"
                    );

                    if (recordCount == 0)
                    {
                        await AddTestData(msConnection);
                        return Json(new { success = true, message = "เพิ่มข้อมูลทดสอบเรียบร้อยแล้ว" });
                    }

                    return Json(new { success = true, message = "ตารางและข้อมูลมีอยู่แล้ว" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        private async Task AddTestData(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            var sessionId = HttpContext.Session.Id;
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            // เพิ่ม Session ทดสอบ
            var sessionSql = @"
                IF NOT EXISTS (SELECT 1 FROM OracleUserSessions WHERE SessionId = @SessionId)
                BEGIN
                    INSERT INTO OracleUserSessions (SessionId, UserId, UserName, ClientIP, UserAgent)
                    VALUES (@SessionId, @UserId, @UserName, @ClientIP, @UserAgent)
                END";

            await connection.ExecuteAsync(sessionSql, new
            {
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                ClientIP = GetClientIP(),
                UserAgent = GetUserAgent()
            });

            // เพิ่มข้อมูลประวัติทดสอบ
            var historySql = @"
                INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, TableName, AffectedRows, ExecutionTimeMs, IsSuccess)
                VALUES 
                    (@SessionId, @UserId, @UserName, 'CONNECTION_TEST', 'SELECT ''Oracle Connection Success!'' FROM DUAL', 'DUAL', 1, 150, 1),
                    (@SessionId, @UserId, @UserName, 'SELECT', 'SELECT * FROM TEmpLeave WHERE EmpNo = :EmpNo', 'TEmpLeave', 5, 245, 1),
                    (@SessionId, @UserId, @UserName, 'UPDATE', 'UPDATE TEmpLeave SET Status = ''Approved'' WHERE EmpNo = :EmpNo', 'TEmpLeave', 2, 180, 1)";

            await connection.ExecuteAsync(historySql, new
            {
                SessionId = sessionId,
                UserId = userId,
                UserName = userName
            });
        }

        private string? GetClientIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        private string? GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Test Browser";
        }

        // ดึงประวัติการ Query แบบจัดกลุ่ม
        [HttpGet]
        public async Task<IActionResult> GetQueryHistory(int page = 1, int pageSize = 50)
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var userId = GetCurrentUserId();

                // ดึงข้อมูลประวัติแบบง่าย (ไม่ใช้ service เพื่อหลีกเลี่ยงปัญหา)
                using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")
                );
                await msConnection.OpenAsync();

                // ตรวจสอบว่าตาราง OracleActivityHistory มีอยู่หรือไม่
                var tableExists = await msConnection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OracleActivityHistory'"
                );

                if (tableExists == 0)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "ตาราง OracleActivityHistory ยังไม่ได้ถูกสร้าง กรุณารัน SQL Script ที่ SQL/CreateOracleHistoryTables.sql",
                            data = new List<object>(),
                            totalGroups = 0,
                            currentPage = page,
                            pageSize = pageSize,
                        }
                    );
                }

                // ดึงข้อมูลประวัติแบบ grouped
                var sql = @"
                    WITH GroupedHistory AS (
                        SELECT 
                            ROW_NUMBER() OVER (ORDER BY MAX(CreatedDate) DESC) as RowNum,
                            ActivityType,
                            COALESCE(TableName, 'Unknown') as TableName,
                            LEFT(SqlCommand, 100) as SqlPreview,
                            SqlCommand as FullSqlCommand,
                            COUNT(*) as ExecutionCount,
                            AVG(CAST(ExecutionTimeMs as FLOAT)) as AvgExecutionTime,
                            MAX(CreatedDate) as LastExecuted,
                            SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessCount,
                            SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) as ErrorCount,
                            SUM(AffectedRows) as TotalAffectedRows,
                            MAX(UserName) as LastUser,
                            -- สร้าง SQL Hash สำหรับ grouping
                            CHECKSUM(UPPER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(SqlCommand, CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' '))))) as SqlHash
                        FROM OracleActivityHistory 
                        WHERE CreatedDate >= DATEADD(day, -30, GETDATE())
                        GROUP BY ActivityType, COALESCE(TableName, 'Unknown'), SqlCommand
                    )
                    SELECT * FROM GroupedHistory 
                    WHERE RowNum BETWEEN @StartRow AND @EndRow
                    ORDER BY LastExecuted DESC";

                var startRow = (page - 1) * pageSize + 1;
                var endRow = page * pageSize;

                var result = await msConnection.QueryAsync(
                    sql,
                    new { StartRow = startRow, EndRow = endRow }
                );

                var historyData = result
                    .Select(r => new
                    {
                        sqlHash = (int)r.SqlHash,
                        activityType = (string)r.ActivityType,
                        tableName = (string)r.TableName,
                        sqlPreview = (string)r.SqlPreview,
                        fullSqlCommand = (string)r.FullSqlCommand,
                        executionCount = (int)r.ExecutionCount,
                        avgExecutionTime = Math.Round((double)r.AvgExecutionTime, 2),
                        lastExecuted = ((DateTime)r.LastExecuted).ToString("dd/MM/yyyy HH:mm:ss"),
                        successCount = (int)r.SuccessCount,
                        errorCount = (int)r.ErrorCount,
                        totalAffectedRows = (int)r.TotalAffectedRows,
                        lastUser = (string)r.LastUser,
                        hasErrors = (int)r.ErrorCount > 0
                    })
                    .ToList();

                // นับจำนวนรายการทั้งหมด
                var totalCount = await msConnection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM OracleActivityHistory WHERE CreatedDate >= DATEADD(day, -30, GETDATE())"
                );

                return Json(
                    new
                    {
                        success = true,
                        data = historyData,
                        totalGroups = totalCount,
                        currentPage = page,
                        pageSize = pageSize,
                        message = historyData.Any()
                            ? $"พบประวัติการใช้งาน {totalCount} รายการ"
                            : "ยังไม่มีประวัติการใช้งาน กรุณาลองใช้งาน Query ก่อน",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ดึงรายละเอียดของกลุ่ม Query สำหรับ child rows
        [HttpGet]
        public async Task<IActionResult> GetQueryGroupDetail(int sqlHash, string activityType)
        {
            try
            {
                using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")
                );
                await msConnection.OpenAsync();

                var sql = @"
                    SELECT 
                        Id,
                        SessionId,
                        UserId,
                        UserName,
                        SqlCommand,
                        Parameters,
                        TableName,
                        AffectedRows,
                        ExecutionTimeMs,
                        IsSuccess,
                        ErrorMessage,
                        ClientIP,
                        UserAgent,
                        CreatedDate
                    FROM OracleActivityHistory 
                    WHERE CreatedDate >= DATEADD(day, -30, GETDATE())
                        AND CHECKSUM(UPPER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(SqlCommand, CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' '))))) = @SqlHash
                        AND ActivityType = @ActivityType
                    ORDER BY CreatedDate DESC";

                var result = await msConnection.QueryAsync(
                    sql,
                    new { SqlHash = sqlHash, ActivityType = activityType }
                );

                var groupDetails = result
                    .Select(r => new
                    {
                        id = (long)r.Id,
                        sessionId = (string)r.SessionId,
                        userId = (string)r.UserId,
                        userName = (string)r.UserName,
                        sqlCommand = (string)r.SqlCommand,
                        parameters = (string)r.Parameters,
                        tableName = (string)r.TableName,
                        affectedRows = (int)r.AffectedRows,
                        executionTimeMs = (int)r.ExecutionTimeMs,
                        isSuccess = (bool)r.IsSuccess,
                        errorMessage = (string)r.ErrorMessage,
                        clientIP = (string)r.ClientIP,
                        userAgent = (string)r.UserAgent,
                        createdDate = ((DateTime)r.CreatedDate).ToString("dd/MM/yyyy HH:mm:ss"),
                        statusBadge = (bool)r.IsSuccess ? 
                            "<span class='badge bg-success'>สำเร็จ</span>" : 
                            "<span class='badge bg-danger'>ผิดพลาด</span>",
                        executionTime = $"{(int)r.ExecutionTimeMs} ms"
                    })
                    .ToList();

                return Json(new { success = true, data = groupDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ดึงข้อมูล Column Info สำหรับตาราง
        [HttpGet]
        public async Task<IActionResult> GetTableColumnInfo(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    return Json(new { success = false, message = "กรุณาระบุชื่อตาราง" });
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // ดึงข้อมูลคอลัมน์จาก Oracle
                var columnSql = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        DATA_LENGTH,
                        DATA_PRECISION,
                        DATA_SCALE,
                        NULLABLE,
                        DATA_DEFAULT,
                        COLUMN_ID
                    FROM USER_TAB_COLUMNS 
                    WHERE TABLE_NAME = UPPER(:tableName)
                    ORDER BY COLUMN_ID";

                var columns = await connection.QueryAsync(columnSql, new { tableName = tableName.ToUpper() });

                var columnInfo = columns.Select(c => new
                {
                    columnName = (string)c.COLUMN_NAME,
                    dataType = FormatDataType((string)c.DATA_TYPE, c.DATA_LENGTH, c.DATA_PRECISION, c.DATA_SCALE),
                    nullable = (string)c.NULLABLE == "Y" ? "YES" : "NO",
                    defaultValue = c.DATA_DEFAULT?.ToString()?.Trim() ?? "-",
                    columnId = (decimal?)c.COLUMN_ID ?? 0
                }).ToList();

                return Json(new { success = true, data = columnInfo, tableName = tableName.ToUpper() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Export ประวัติเป็น Excel/CSV
        [HttpGet]
        public async Task<IActionResult> ExportHistory(string format = "excel", int days = 30)
        {
            try
            {
                using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")
                );
                await msConnection.OpenAsync();

                var sql = @"
                    SELECT 
                        ActivityType,
                        COALESCE(TableName, 'Unknown') as TableName,
                        SqlCommand,
                        Parameters,
                        AffectedRows,
                        ExecutionTimeMs,
                        IsSuccess,
                        ErrorMessage,
                        UserName,
                        CreatedDate,
                        ClientIP
                    FROM OracleActivityHistory 
                    WHERE CreatedDate >= DATEADD(day, -@Days, GETDATE())
                    ORDER BY CreatedDate DESC";

                var result = await msConnection.QueryAsync(sql, new { Days = days });

                var fileName = $"OracleHistory_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                if (format.ToLower() == "csv")
                {
                    var csv = ConvertToCsv(result);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{fileName}.csv");
                }
                else
                {
                    // สำหรับ Excel format (ใช้ CSV format ชั่วคราว)
                    var csv = ConvertToCsv(result);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "application/vnd.ms-excel", $"{fileName}.xls");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // คลี Copy SQL สำหรับใช้ซ้ำ
        [HttpPost]
        public IActionResult GetReusableQuery(int sqlHash, string activityType)
        {
            try
            {
                // ดึง SQL command จาก hash
                using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    _configuration.GetConnectionString("DefaultConnection")
                );
                msConnection.Open();

                var sql = @"
                    SELECT TOP 1 SqlCommand, Parameters 
                    FROM OracleActivityHistory 
                    WHERE CHECKSUM(UPPER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(SqlCommand, CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' '))))) = @SqlHash
                        AND ActivityType = @ActivityType
                    ORDER BY CreatedDate DESC";

                var result = msConnection.QueryFirstOrDefault(sql, new { SqlHash = sqlHash, ActivityType = activityType });

                if (result != null)
                {
                    return Json(new 
                    { 
                        success = true, 
                        sqlCommand = (string)result.SqlCommand,
                        parameters = (string)result.Parameters ?? "{}"
                    });
                }

                return Json(new { success = false, message = "ไม่พบ SQL Command" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        private string ConvertToCsv(IEnumerable<dynamic> data)
        {
            var csv = new System.Text.StringBuilder();
            
            // Header
            csv.AppendLine("ActivityType,TableName,SqlCommand,Parameters,AffectedRows,ExecutionTimeMs,IsSuccess,ErrorMessage,UserName,CreatedDate,ClientIP");

            // Data rows
            foreach (var item in data)
            {
                csv.AppendLine($"{EscapeCsvField(item.ActivityType)},{EscapeCsvField(item.TableName)},{EscapeCsvField(item.SqlCommand)},{EscapeCsvField(item.Parameters)},{item.AffectedRows},{item.ExecutionTimeMs},{item.IsSuccess},{EscapeCsvField(item.ErrorMessage)},{EscapeCsvField(item.UserName)},{item.CreatedDate},{EscapeCsvField(item.ClientIP)}");
            }

            return csv.ToString();
        }

        private string EscapeCsvField(object field)
        {
            if (field == null) return "";
            
            var value = field.ToString();
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }

        // ดึงรายละเอียด Query เฉพาะ
        [HttpGet]
        public async Task<IActionResult> GetQueryDetail(long id)
        {
            try
            {
                var activity = await _historyService.GetActivityByIdAsync(id);
                if (activity == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลประวัติการ Query" });
                }

                return Json(
                    new
                    {
                        success = true,
                        data = new
                        {
                            id = activity.Id,
                            sessionId = activity.SessionId,
                            userId = activity.UserId,
                            userName = activity.UserName,
                            activityType = activity.ActivityType,
                            sqlCommand = activity.SqlCommand,
                            parameters = activity.Parameters,
                            tableName = activity.TableName,
                            affectedRows = activity.AffectedRows,
                            executionTimeMs = activity.ExecutionTimeMs,
                            isSuccess = activity.IsSuccess,
                            errorMessage = activity.ErrorMessage,
                            clientIP = activity.ClientIP,
                            userAgent = activity.UserAgent,
                            createdDate = activity.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss"),
                            dataChanges = activity
                                .DataChanges?.Select(dc => new
                                {
                                    tableName = dc.TableName,
                                    columnName = dc.ColumnName,
                                    recordIdentifier = dc.RecordIdentifier,
                                    oldValue = dc.OldValue,
                                    newValue = dc.NewValue,
                                    dataType = dc.DataType,
                                })
                                .ToList(),
                            queryPerformances = activity
                                .QueryPerformances?.Select(qp => new
                                {
                                    sqlHash = qp.SqlHash,
                                    queryType = qp.QueryType,
                                    rowsReturned = qp.RowsReturned,
                                    executionTimeMs = qp.ExecutionTimeMs,
                                    memoryUsedKB = qp.MemoryUsedKB,
                                    cacheHit = qp.CacheHit,
                                })
                                .ToList(),
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ดึงสถิติการใช้งาน Dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _historyService.GetDashboardStatsAsync();
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ทดสอบการเชื่อมต่อ Oracle Database
        [HttpPost]
        public async Task<IActionResult> TestConnection()
        {
            var stopwatch = Stopwatch.StartNew();
            var sessionId = HttpContext.Session.Id;
            var isSuccess = false;
            string? errorMessage = null;

            try
            {
                // สร้าง/อัพเดท session
                await _historyService.CreateOrUpdateSessionAsync(
                    new CreateSessionRequest
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                    }
                );

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // ทดสอบ query พื้นฐาน
                var result = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT 'Oracle Connection Success!' FROM DUAL"
                );

                isSuccess = true;
                stopwatch.Stop();

                // บันทึก history แบบ manual
                try
                {
                    using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    );
                    await msConnection.OpenAsync();
                    
                    var historySql = @"
                        INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, TableName, AffectedRows, ExecutionTimeMs, IsSuccess, ClientIP, UserAgent, CreatedDate)
                        VALUES (@SessionId, @UserId, @UserName, @ActivityType, @SqlCommand, @TableName, @AffectedRows, @ExecutionTimeMs, @IsSuccess, @ClientIP, @UserAgent, GETDATE())";
                    
                    await msConnection.ExecuteAsync(historySql, new
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = "CONNECTION_TEST",
                        SqlCommand = "SELECT 'Oracle Connection Success!' FROM DUAL",
                        TableName = "DUAL",
                        AffectedRows = 1,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ClientIP = GetClientIP(),
                        UserAgent = GetUserAgent()
                    });
                }
                catch (Exception historyEx)
                {
                    Console.WriteLine($"History logging error: {historyEx.Message}");
                }

                return Json(
                    new
                    {
                        success = true,
                        message = result,
                        connectionState = connection.State.ToString(),
                    }
                );
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                stopwatch.Stop();

                // บันทึก history สำหรับ error แบบ manual
                try
                {
                    using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    );
                    await msConnection.OpenAsync();
                    
                    var historySql = @"
                        INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, TableName, AffectedRows, ExecutionTimeMs, IsSuccess, ErrorMessage, ClientIP, UserAgent, CreatedDate)
                        VALUES (@SessionId, @UserId, @UserName, @ActivityType, @SqlCommand, @TableName, @AffectedRows, @ExecutionTimeMs, @IsSuccess, @ErrorMessage, @ClientIP, @UserAgent, GETDATE())";
                    
                    await msConnection.ExecuteAsync(historySql, new
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = "CONNECTION_TEST",
                        SqlCommand = "SELECT 'Oracle Connection Success!' FROM DUAL",
                        TableName = "DUAL",
                        AffectedRows = 0,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                        ClientIP = GetClientIP(),
                        UserAgent = GetUserAgent()
                    });
                }
                catch (Exception historyEx)
                {
                    Console.WriteLine($"History logging error: {historyEx.Message}");
                }

                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ตัวอย่างการดึงข้อมูลจาก Oracle
        [HttpGet]
        public async Task<IActionResult> GetOracleData(string tableName = "DUAL")
        {
            try
            {
                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // ตัวอย่างการ query ข้อมูล (แก้ไข SQL ตามตารางจริงของคุณ)
                var sql = $"SELECT COUNT(*) as RecordCount FROM {tableName}";
                var result = await connection.QueryFirstOrDefaultAsync<int>(sql);

                return Json(
                    new
                    {
                        success = true,
                        tableName = tableName,
                        recordCount = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ตัวอย่างการ Execute PL/SQL Procedure
        [HttpPost]
        public async Task<IActionResult> ExecuteProcedure(
            string procedureName,
            string parameters = ""
        )
        {
            try
            {
                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // ตัวอย่างการเรียก Stored Procedure
                var sql = $"BEGIN {procedureName}({parameters}); END;";
                var result = await connection.ExecuteAsync(sql);

                return Json(
                    new
                    {
                        success = true,
                        message = $"เรียก {procedureName} สำเร็จ",
                        affectedRows = result,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // รัน SQL Query แบบยืดหยุ่นและแสดงผลเป็น DataTable
        [HttpPost]
        public async Task<IActionResult> ExecuteQuery(string sqlQuery, string parameters = "")
        {
            var stopwatch = Stopwatch.StartNew();
            var sessionId = HttpContext.Session.Id;
            var isSuccess = false;
            string? errorMessage = null;
            int rowCount = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    return Json(new { success = false, message = "กรุณาระบุ SQL Query" });
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // แปลง parameters จาก JSON string เป็น object ที่ Dapper สามารถใช้ได้
                object queryParams = null;

                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    try
                    {
                        var deserializedParams = JsonConvert.DeserializeObject(parameters);
                        queryParams = ConvertToAnonymousObject(deserializedParams);
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"รูปแบบ Parameters ไม่ถูกต้อง (ควรเป็น JSON): {ex.Message}",
                            }
                        );
                    }
                }

                // Execute query และแปลงเป็น DataTable
                var result = await connection.QueryAsync(sqlQuery, queryParams);
                var dataTable = ConvertToDataTable(result);
                var columnStats = GetColumnStatistics(dataTable);

                rowCount = dataTable.Rows.Count;
                isSuccess = true;
                stopwatch.Stop();

                // บันทึก history แบบ manual สำหรับความมั่นใจ
                try
                {
                    using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    );
                    await msConnection.OpenAsync();
                    
                    var historySql = @"
                        INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, Parameters, TableName, AffectedRows, ExecutionTimeMs, IsSuccess, ClientIP, UserAgent, CreatedDate)
                        VALUES (@SessionId, @UserId, @UserName, @ActivityType, @SqlCommand, @Parameters, @TableName, @AffectedRows, @ExecutionTimeMs, @IsSuccess, @ClientIP, @UserAgent, GETDATE())";
                    
                    await msConnection.ExecuteAsync(historySql, new
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = "SELECT",
                        SqlCommand = sqlQuery,
                        Parameters = parameters,
                        TableName = ExtractTableNameFromSql(sqlQuery),
                        AffectedRows = rowCount,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ClientIP = GetClientIP(),
                        UserAgent = GetUserAgent()
                    });
                }
                catch (Exception historyEx)
                {
                    // ไม่ให้ error ของ history กระทบกับผลลัพธ์หลัก
                    Console.WriteLine($"History logging error: {historyEx.Message}");
                }

                return Json(
                    new
                    {
                        success = true,
                        message = $"พบข้อมูล {dataTable.Rows.Count} รายการ",
                        columns = GetColumnNames(dataTable),
                        data = ConvertDataTableToJson(dataTable),
                        sql = sqlQuery,
                        columnStatistics = columnStats,
                    }
                );
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                stopwatch.Stop();

                // บันทึก history สำหรับ error แบบ manual
                try
                {
                    using var msConnection = new Microsoft.Data.SqlClient.SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    );
                    await msConnection.OpenAsync();
                    
                    var historySql = @"
                        INSERT INTO OracleActivityHistory (SessionId, UserId, UserName, ActivityType, SqlCommand, Parameters, TableName, AffectedRows, ExecutionTimeMs, IsSuccess, ErrorMessage, ClientIP, UserAgent, CreatedDate)
                        VALUES (@SessionId, @UserId, @UserName, @ActivityType, @SqlCommand, @Parameters, @TableName, @AffectedRows, @ExecutionTimeMs, @IsSuccess, @ErrorMessage, @ClientIP, @UserAgent, GETDATE())";
                    
                    await msConnection.ExecuteAsync(historySql, new
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = "SELECT",
                        SqlCommand = sqlQuery,
                        Parameters = parameters,
                        TableName = ExtractTableNameFromSql(sqlQuery),
                        AffectedRows = 0,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                        ClientIP = GetClientIP(),
                        UserAgent = GetUserAgent()
                    });
                }
                catch (Exception historyEx)
                {
                    Console.WriteLine($"History logging error: {historyEx.Message}");
                }

                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ตรวจสอบข้อมูลก่อน Update
        [HttpPost]
        public async Task<IActionResult> PreviewUpdate(
            string tableName,
            string whereClause,
            string updateFields,
            string parameters = ""
        )
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(tableName)
                    || string.IsNullOrWhiteSpace(updateFields)
                    || string.IsNullOrWhiteSpace(whereClause)
                )
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // แปลง parameters จาก JSON string เป็น object ที่ Dapper สามารถใช้ได้
                object queryParams = null;
                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    try
                    {
                        var deserializedParams = JsonConvert.DeserializeObject(parameters);
                        queryParams = ConvertToAnonymousObject(deserializedParams);
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"รูปแบบ Parameters ไม่ถูกต้อง: {ex.Message}",
                            }
                        );
                    }
                }

                // ตรวจสอบจำนวนข้อมูลที่จะได้รับผลกระทบ
                var countSql = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";
                var affectedCount = await connection.QueryFirstOrDefaultAsync<int>(
                    countSql,
                    queryParams
                );

                // ดึงข้อมูลที่จะได้รับผลกระทบ (สูงสุด 10 รายการ)
                var previewSql = $"SELECT * FROM {tableName} WHERE {whereClause} AND ROWNUM <= 10";
                var previewData = await connection.QueryAsync(previewSql, queryParams);
                var dataTable = ConvertToDataTable(previewData);

                return Json(
                    new
                    {
                        success = true,
                        affectedCount = affectedCount,
                        previewData = ConvertDataTableToJson(dataTable),
                        columns = GetColumnNames(dataTable),
                        updateSql = $"UPDATE {tableName} SET {updateFields} WHERE {whereClause}",
                        message = $"จะมีผลกระทบต่อข้อมูล {affectedCount} รายการ",
                        requiresConfirmation = affectedCount
                            > 1 // เพิ่มการแจ้งเตือนสำหรับหลายแถว
                        ,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ตรวจสอบข้อมูลก่อน Delete
        [HttpPost]
        public async Task<IActionResult> PreviewDelete(
            string tableName,
            string whereClause,
            string parameters = ""
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereClause))
                {
                    return Json(
                        new { success = false, message = "กรุณาระบุชื่อตารางและเงื่อนไข WHERE" }
                    );
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // แปลง parameters จาก JSON string เป็น object ที่ Dapper สามารถใช้ได้
                object queryParams = null;
                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    try
                    {
                        var deserializedParams = JsonConvert.DeserializeObject(parameters);
                        queryParams = ConvertToAnonymousObject(deserializedParams);
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"รูปแบบ Parameters ไม่ถูกต้อง: {ex.Message}",
                            }
                        );
                    }
                }

                // ตรวจสอบจำนวนข้อมูลที่จะถูกลบ
                var countSql = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";
                var affectedCount = await connection.QueryFirstOrDefaultAsync<int>(
                    countSql,
                    queryParams
                );

                // ดึงข้อมูลที่จะถูกลบ (สูงสุด 10 รายการ)
                var previewSql = $"SELECT * FROM {tableName} WHERE {whereClause} AND ROWNUM <= 10";
                var previewData = await connection.QueryAsync(previewSql, queryParams);
                var dataTable = ConvertToDataTable(previewData);

                return Json(
                    new
                    {
                        success = true,
                        affectedCount = affectedCount,
                        previewData = ConvertDataTableToJson(dataTable),
                        columns = GetColumnNames(dataTable),
                        deleteSql = $"DELETE FROM {tableName} WHERE {whereClause}",
                        message = $"จะลบข้อมูล {affectedCount} รายการ",
                        requiresConfirmation = affectedCount
                            > 1 // เพิ่มการแจ้งเตือนสำหรับหลายแถว
                        ,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // อัพเดทข้อมูล
        [HttpPost]
        public async Task<IActionResult> UpdateRecord(
            string tableName,
            string whereClause,
            string updateFields,
            string parameters = ""
        )
        {
            var stopwatch = Stopwatch.StartNew();
            var sessionId = HttpContext.Session.Id;
            var isSuccess = false;
            string? errorMessage = null;
            int affectedRows = 0;

            try
            {
                if (
                    string.IsNullOrWhiteSpace(tableName)
                    || string.IsNullOrWhiteSpace(updateFields)
                    || string.IsNullOrWhiteSpace(whereClause)
                )
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "กรุณาระบุชื่อตาราง, ฟิลด์ที่จะอัพเดท และเงื่อนไข WHERE",
                        }
                    );
                }

                var sql = $"UPDATE {tableName} SET {updateFields} WHERE {whereClause}";

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // แปลง parameters จาก JSON string เป็น object ที่ Dapper สามารถใช้ได้
                object queryParams = null;
                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    try
                    {
                        var deserializedParams = JsonConvert.DeserializeObject(parameters);
                        queryParams = ConvertToAnonymousObject(deserializedParams);
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"รูปแบบ Parameters ไม่ถูกต้อง: {ex.Message}",
                            }
                        );
                    }
                }

                var result = await connection.ExecuteAsync(sql, queryParams);
                affectedRows = result;
                isSuccess = true;
                stopwatch.Stop();

                // บันทึก history
                await _historyService.LogActivityAsync(
                    new LogActivityRequest
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = OracleHistoryConstants.ActivityTypes.UPDATE,
                        SqlCommand = sql,
                        Parameters = parameters,
                        TableName = tableName,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        AffectedRows = affectedRows,
                    }
                );

                return Json(
                    new
                    {
                        success = true,
                        message = $"อัพเดทข้อมูลสำเร็จ {result} รายการ",
                        affectedRows = result,
                    }
                );
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                stopwatch.Stop();

                // บันทึก history สำหรับ error
                await _historyService.LogActivityAsync(
                    new LogActivityRequest
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = OracleHistoryConstants.ActivityTypes.UPDATE,
                        SqlCommand = $"UPDATE {tableName} SET {updateFields} WHERE {whereClause}",
                        Parameters = parameters,
                        TableName = tableName,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                    }
                );

                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ลบข้อมูล
        [HttpPost]
        public async Task<IActionResult> DeleteRecord(
            string tableName,
            string whereClause,
            string parameters = ""
        )
        {
            var stopwatch = Stopwatch.StartNew();
            var sessionId = HttpContext.Session.Id;
            var isSuccess = false;
            string? errorMessage = null;
            int affectedRows = 0;
            var sql = $"DELETE FROM {tableName} WHERE {whereClause}";

            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(whereClause))
                {
                    return Json(
                        new { success = false, message = "กรุณาระบุชื่อตารางและเงื่อนไข WHERE" }
                    );
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // แปลง parameters จาก JSON string เป็น object ที่ Dapper สามารถใช้ได้
                object queryParams = null;
                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    try
                    {
                        var deserializedParams = JsonConvert.DeserializeObject(parameters);
                        queryParams = ConvertToAnonymousObject(deserializedParams);
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"รูปแบบ Parameters ไม่ถูกต้อง: {ex.Message}",
                            }
                        );
                    }
                }

                var result = await connection.ExecuteAsync(sql, queryParams);
                affectedRows = result;
                isSuccess = true;
                stopwatch.Stop();

                // บันทึก history
                await _historyService.LogActivityAsync(
                    new LogActivityRequest
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = OracleHistoryConstants.ActivityTypes.DELETE,
                        SqlCommand = sql,
                        Parameters = parameters,
                        TableName = tableName,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        AffectedRows = affectedRows,
                    }
                );

                return Json(
                    new
                    {
                        success = true,
                        message = $"ลบข้อมูลสำเร็จ {result} รายการ",
                        affectedRows = result,
                    }
                );
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
                stopwatch.Stop();

                // บันทึก history สำหรับ error
                await _historyService.LogActivityAsync(
                    new LogActivityRequest
                    {
                        SessionId = sessionId,
                        UserId = GetCurrentUserId(),
                        UserName = GetCurrentUserName(),
                        ActivityType = OracleHistoryConstants.ActivityTypes.DELETE,
                        SqlCommand = sql,
                        Parameters = parameters,
                        TableName = tableName,
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                    }
                );

                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ดึงข้อมูล Schema ของตาราง
        [HttpGet]
        public async Task<IActionResult> GetTableSchema(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    return Json(new { success = false, message = "กรุณาระบุชื่อตาราง" });
                }

                using var connection = _oracleContext.Database.GetDbConnection();
                await connection.OpenAsync();

                // Query ข้อมูลคอลัมน์จาก Oracle Data Dictionary
                var columnSql =
                    @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        DATA_LENGTH,
                        DATA_PRECISION,
                        DATA_SCALE,
                        NULLABLE,
                        DATA_DEFAULT,
                        COLUMN_ID,
                        COMMENTS
                    FROM USER_TAB_COLUMNS utc
                    LEFT JOIN USER_COL_COMMENTS ucc ON utc.TABLE_NAME = ucc.TABLE_NAME AND utc.COLUMN_NAME = ucc.COLUMN_NAME
                    WHERE utc.TABLE_NAME = UPPER(:tableName)
                    ORDER BY COLUMN_ID";

                var columns = await connection.QueryAsync(
                    columnSql,
                    new { tableName = tableName.ToUpper() }
                );

                // Query ข้อมูล Primary Key
                var pkSql =
                    @"
                    SELECT ucc.COLUMN_NAME
                    FROM USER_CONSTRAINTS uc
                    JOIN USER_CONS_COLUMNS ucc ON uc.CONSTRAINT_NAME = ucc.CONSTRAINT_NAME
                    WHERE uc.TABLE_NAME = UPPER(:tableName) 
                    AND uc.CONSTRAINT_TYPE = 'P'
                    ORDER BY ucc.POSITION";

                var primaryKeys = await connection.QueryAsync<string>(
                    pkSql,
                    new { tableName = tableName.ToUpper() }
                );

                // Query ข้อมูล Foreign Key
                var fkSql =
                    @"
                    SELECT 
                        ucc.COLUMN_NAME,
                        uc.R_CONSTRAINT_NAME,
                        r_uc.TABLE_NAME as REFERENCED_TABLE,
                        r_ucc.COLUMN_NAME as REFERENCED_COLUMN
                    FROM USER_CONSTRAINTS uc
                    JOIN USER_CONS_COLUMNS ucc ON uc.CONSTRAINT_NAME = ucc.CONSTRAINT_NAME
                    JOIN USER_CONSTRAINTS r_uc ON uc.R_CONSTRAINT_NAME = r_uc.CONSTRAINT_NAME
                    JOIN USER_CONS_COLUMNS r_ucc ON r_uc.CONSTRAINT_NAME = r_ucc.CONSTRAINT_NAME
                    WHERE uc.TABLE_NAME = UPPER(:tableName) 
                    AND uc.CONSTRAINT_TYPE = 'R'
                    ORDER BY ucc.POSITION";

                var foreignKeys = await connection.QueryAsync(
                    fkSql,
                    new { tableName = tableName.ToUpper() }
                );

                // Query ข้อมูล Index
                var indexSql =
                    @"
                    SELECT 
                        ui.INDEX_NAME,
                        ui.UNIQUENESS,
                        uic.COLUMN_NAME,
                        uic.COLUMN_POSITION
                    FROM USER_INDEXES ui
                    JOIN USER_IND_COLUMNS uic ON ui.INDEX_NAME = uic.INDEX_NAME
                    WHERE ui.TABLE_NAME = UPPER(:tableName)
                    ORDER BY ui.INDEX_NAME, uic.COLUMN_POSITION";

                var indexes = await connection.QueryAsync(
                    indexSql,
                    new { tableName = tableName.ToUpper() }
                );

                // Query ข้อมูลตาราง
                var tableSql =
                    @"
                    SELECT 
                        TABLE_NAME,
                        NUM_ROWS,
                        BLOCKS,
                        AVG_ROW_LEN,
                        LAST_ANALYZED
                    FROM USER_TABLES 
                    WHERE TABLE_NAME = UPPER(:tableName)";

                var tableInfo = await connection.QueryFirstOrDefaultAsync(
                    tableSql,
                    new { tableName = tableName.ToUpper() }
                );

                // จัดรูปแบบข้อมูล
                var columnList = columns
                    .Select(c => new
                    {
                        columnName = (string)c.COLUMN_NAME,
                        dataType = FormatDataType(
                            (string)c.DATA_TYPE,
                            c.DATA_LENGTH,
                            c.DATA_PRECISION,
                            c.DATA_SCALE
                        ),
                        nullable = (string)c.NULLABLE == "Y" ? "YES" : "NO",
                        defaultValue = c.DATA_DEFAULT?.ToString()?.Trim(),
                        comments = (string)c.COMMENTS,
                        columnId = (decimal?)c.COLUMN_ID,
                        isPrimaryKey = primaryKeys.Contains((string)c.COLUMN_NAME),
                        foreignKeyInfo = foreignKeys
                            .Where(fk => (string)fk.COLUMN_NAME == (string)c.COLUMN_NAME)
                            .FirstOrDefault(),
                    })
                    .ToList();

                var indexList = indexes
                    .GroupBy(i => (string)i.INDEX_NAME)
                    .Select(g => new
                    {
                        indexName = g.Key,
                        isUnique = (string)g.First().UNIQUENESS == "UNIQUE",
                        columns = g.OrderBy(x => (decimal)x.COLUMN_POSITION)
                            .Select(x => (string)x.COLUMN_NAME)
                            .ToList(),
                    })
                    .ToList();

                return Json(
                    new
                    {
                        success = true,
                        data = new
                        {
                            tableName = tableName.ToUpper(),
                            tableInfo = tableInfo,
                            columns = columnList,
                            primaryKeys = primaryKeys.ToList(),
                            foreignKeys = foreignKeys.ToList(),
                            indexes = indexList,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Helper method สำหรับจัดรูปแบบ Data Type
        private string FormatDataType(
            string dataType,
            object dataLength,
            object dataPrecision,
            object dataScale
        )
        {
            switch (dataType?.ToUpper())
            {
                case "VARCHAR2":
                case "CHAR":
                case "NVARCHAR2":
                case "NCHAR":
                    return $"{dataType}({dataLength})";

                case "NUMBER":
                    if (dataPrecision != null && dataPrecision != DBNull.Value)
                    {
                        var precision = Convert.ToInt32(dataPrecision);
                        if (dataScale != null && dataScale != DBNull.Value)
                        {
                            var scale = Convert.ToInt32(dataScale);
                            return scale > 0
                                ? $"NUMBER({precision},{scale})"
                                : $"NUMBER({precision})";
                        }
                        return $"NUMBER({precision})";
                    }
                    return "NUMBER";

                case "RAW":
                    return $"RAW({dataLength})";

                default:
                    return dataType ?? "UNKNOWN";
            }
        }

        // แยกชื่อตารางจาก SQL Query
        [HttpPost]
        public IActionResult ExtractTableName(string sqlQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    return Json(
                        new { success = false, message = "SQL Query ไม่สามารถเป็นค่าว่างได้" }
                    );
                }

                var tableName = ExtractTableNameFromSql(sqlQuery);
                return Json(new { success = true, tableName = tableName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง WHERE Clause จากข้อมูลที่เลือก
        [HttpPost]
        public IActionResult GenerateWhereClause(Dictionary<string, object> selectedFields)
        {
            try
            {
                if (selectedFields == null || !selectedFields.Any())
                {
                    return Json(new { success = false, message = "ไม่มีข้อมูลที่เลือก" });
                }

                var whereConditions = new List<string>();

                foreach (var field in selectedFields)
                {
                    if (field.Value == null || string.IsNullOrEmpty(field.Value.ToString()))
                    {
                        whereConditions.Add($"{field.Key} IS NULL");
                    }
                    else
                    {
                        // ตรวจสอบประเภทข้อมูลและจัดรูปแบบให้เหมาะสม
                        var value = field.Value.ToString();
                        if (IsNumericValue(value))
                        {
                            whereConditions.Add($"{field.Key} = {value}");
                        }
                        else if (IsDateValue(value))
                        {
                            whereConditions.Add($"{field.Key} = DATE '{value}'");
                        }
                        else
                        {
                            // Escape single quotes สำหรับ string values
                            var escapedValue = value.Replace("'", "''");
                            whereConditions.Add($"{field.Key} = '{escapedValue}'");
                        }
                    }
                }

                var whereClause = string.Join(" AND ", whereConditions);

                return Json(
                    new
                    {
                        success = true,
                        whereClause = whereClause,
                        conditionCount = whereConditions.Count,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // คำนวณสถิติคอลัมน์ใน server-side
        [HttpPost]
        public IActionResult CalculateColumnStatistics(
            List<Dictionary<string, object>> data,
            List<string> columns
        )
        {
            try
            {
                if (data == null || !data.Any() || columns == null || !columns.Any())
                {
                    return Json(new { success = false, message = "ไม่มีข้อมูลสำหรับคำนวณสถิติ" });
                }

                var statistics = CalculateColumnStatisticsInternal(data, columns);

                return Json(new { success = true, statistics = statistics });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง HTML สำหรับแสดงสถิติคอลัมน์
        [HttpPost]
        public IActionResult RenderColumnStatistics(object columnStats)
        {
            try
            {
                var stats = JsonConvert.DeserializeObject<ColumnStatisticsModel>(
                    JsonConvert.SerializeObject(columnStats)
                );

                if (stats == null)
                {
                    return Json(
                        new { success = false, message = "ไม่สามารถประมวลผลข้อมูลสถิติได้" }
                    );
                }

                var html = GenerateColumnStatisticsHtml(stats);

                return Json(
                    new
                    {
                        success = true,
                        html = html,
                        totalColumns = stats.TotalColumns,
                        displayedColumns = stats.DisplayedColumns,
                        hiddenColumns = stats.TotalColumns - stats.DisplayedColumns,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง Preview Modal HTML
        [HttpPost]
        public IActionResult RenderPreviewModal(string modalType, object data)
        {
            try
            {
                string html = string.Empty;

                switch (modalType?.ToLower())
                {
                    case "update":
                        html = GenerateUpdatePreviewHtml(data);
                        break;
                    case "delete":
                        html = GenerateDeletePreviewHtml(data);
                        break;
                    case "columnstats":
                        html = GenerateColumnStatsModalHtml(data);
                        break;
                    default:
                        return Json(new { success = false, message = "ประเภท Modal ไม่ถูกต้อง" });
                }

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง DataTable HTML สำหรับ Query History
        [HttpPost]
        public IActionResult RenderQueryHistoryTable(List<object> data)
        {
            try
            {
                var html = new System.Text.StringBuilder();

                html.Append(
                    "<table id=\"queryHistoryTable\" class=\"table table-striped table-hover w-100\">"
                );
                html.Append("<thead class=\"table-dark\">");
                html.Append("<tr>");
                html.Append("<th>ประเภท</th>");
                html.Append("<th>SQL Command</th>");
                html.Append("<th>ตาราง</th>");
                html.Append("<th>จำนวนครั้ง</th>");
                html.Append("<th>เวลาเฉลี่ย</th>");
                html.Append("<th>สำเร็จ/ผิดพลาด</th>");
                html.Append("<th>ล่าสุด</th>");
                html.Append("<th>การกระทำ</th>");
                html.Append("</tr>");
                html.Append("</thead>");
                html.Append("<tbody>");

                foreach (dynamic item in data)
                {
                    var activityTypeBadge = GetActivityTypeBadge(item.activityType?.ToString());
                    var sqlPreview = TruncateText(item.sqlCommand?.ToString(), 100);
                    var lastExecuted = DateTime.TryParse(
                        item.lastExecuted?.ToString(),
                        out DateTime date
                    )
                        ? date.ToString("dd/MM/yyyy HH:mm")
                        : "-";

                    html.Append("<tr>");
                    html.Append($"<td>{activityTypeBadge}</td>");
                    html.Append($"<td><span title=\"{item.sqlCommand}\">{sqlPreview}</span></td>");
                    html.Append($"<td>{item.tableName ?? "-"}</td>");
                    html.Append($"<td>{item.executionCount} ครั้ง</td>");
                    html.Append($"<td>{item.avgExecutionTime:F0} ms</td>");
                    html.Append($"<td>{item.successCount}/{item.errorCount}</td>");
                    html.Append($"<td>{lastExecuted}</td>");
                    html.Append($"<td>{GenerateActionButtons(item)}</td>");
                    html.Append("</tr>");
                }

                html.Append("</tbody>");
                html.Append("</table>");

                return Json(new { success = true, html = html.ToString() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง Action Buttons สำหรับแต่ละแถว
        private string GenerateActionButtons(dynamic item)
        {
            var buttons = new System.Text.StringBuilder();

            buttons.Append("<div class=\"btn-group\" role=\"group\">");

            // ปุ่มดูรายละเอียด
            buttons.Append($"<button class=\"btn btn-xs btn-outline-info view-group-detail\" ");
            buttons.Append(
                $"data-sql-hash=\"{item.sqlHash}\" data-activity-type=\"{item.activityType}\" title=\"ดูรายละเอียด\">"
            );
            buttons.Append("<i class=\"fas fa-info-circle\"></i>");
            buttons.Append("</button>");

            // ปุ่มใช้ Query ซ้ำ
            var sqlCommand = item.sqlCommand?.ToString()?.Replace("'", "\\'") ?? "";
            buttons.Append($"<button class=\"btn btn-xs btn-outline-success reuse-query\" ");
            buttons.Append(
                $"data-sql=\"{sqlCommand}\" data-activity-type=\"{item.activityType}\" title=\"ใช้ใหม่\">"
            );
            buttons.Append("<i class=\"fas fa-redo\"></i>");
            buttons.Append("</button>");

            buttons.Append("</div>");

            return buttons.ToString();
        }

        // สร้าง Badge สำหรับประเภท Activity
        private string GetActivityTypeBadge(string activityType)
        {
            return activityType?.ToUpper() switch
            {
                "SELECT" => "<span class=\"badge bg-success\">SELECT</span>",
                "INSERT" => "<span class=\"badge bg-primary\">INSERT</span>",
                "UPDATE" => "<span class=\"badge bg-warning text-dark\">UPDATE</span>",
                "DELETE" => "<span class=\"badge bg-danger\">DELETE</span>",
                "CONNECTION_TEST" => "<span class=\"badge bg-info\">CONNECTION</span>",
                "PROCEDURE_CALL" => "<span class=\"badge bg-secondary\">PROCEDURE</span>",
                _ => "<span class=\"badge bg-light text-dark\">UNKNOWN</span>",
            };
        }

        // ตัดข้อความให้สั้นลง
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "-";

            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        // สร้าง Tab Content ต่างๆ
        [HttpGet]
        public IActionResult GetTabContent(string tabType)
        {
            try
            {
                string html = tabType?.ToLower() switch
                {
                    "query" => "<div class=\"alert alert-info\">Query Builder Tab</div>",
                    "history" => "<div class=\"alert alert-info\">History Tab</div>",
                    "connection" => "<div class=\"alert alert-info\">Connection Tab</div>",
                    _ => "<div class=\"alert alert-danger\">Invalid tab type</div>",
                };

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Helper Methods
        private string ExtractTableNameFromSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return "UNKNOWN_TABLE";

            try
            {
                // ลบ comments และ whitespace ที่ไม่จำเป็น
                var cleanSql = sql.Trim().ToUpper();

                // หา FROM clause
                var fromMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanSql,
                    @"FROM\s+([A-Z_][A-Z0-9_]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (fromMatch.Success && fromMatch.Groups.Count > 1)
                {
                    return fromMatch.Groups[1].Value;
                }

                // หา UPDATE clause
                var updateMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanSql,
                    @"UPDATE\s+([A-Z_][A-Z0-9_]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (updateMatch.Success && updateMatch.Groups.Count > 1)
                {
                    return updateMatch.Groups[1].Value;
                }

                // หา DELETE clause
                var deleteMatch = System.Text.RegularExpressions.Regex.Match(
                    cleanSql,
                    @"DELETE\s+FROM\s+([A-Z_][A-Z0-9_]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (deleteMatch.Success && deleteMatch.Groups.Count > 1)
                {
                    return deleteMatch.Groups[1].Value;
                }

                return "UNKNOWN_TABLE";
            }
            catch
            {
                return "UNKNOWN_TABLE";
            }
        }

        private bool IsNumericValue(string value)
        {
            return decimal.TryParse(value, out _)
                || int.TryParse(value, out _)
                || double.TryParse(value, out _);
        }

        private bool IsDateValue(string value)
        {
            return DateTime.TryParse(value, out _)
                || DateTime.TryParseExact(
                    value,
                    new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" },
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out _
                );
        }

        private object CalculateColumnStatisticsInternal(
            List<Dictionary<string, object>> data,
            List<string> columns
        )
        {
            var columnStats = new List<object>();
            int totalRows = data.Count;

            foreach (var column in columns)
            {
                int nullCount = 0;
                int emptyCount = 0;
                int validCount = 0;

                foreach (var row in data)
                {
                    if (row.TryGetValue(column, out var value))
                    {
                        if (value == null || value == DBNull.Value)
                        {
                            nullCount++;
                        }
                        else if (string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            emptyCount++;
                        }
                        else
                        {
                            validCount++;
                        }
                    }
                    else
                    {
                        nullCount++;
                    }
                }

                var emptyPercentage =
                    totalRows > 0
                        ? Math.Round((double)(nullCount + emptyCount) / totalRows * 100, 2)
                        : 0;

                columnStats.Add(
                    new
                    {
                        columnName = column,
                        totalRows = totalRows,
                        nullCount = nullCount,
                        emptyCount = emptyCount,
                        validCount = validCount,
                        emptyPercentage = emptyPercentage,
                        isCompletelyEmpty = validCount == 0,
                    }
                );
            }

            return new
            {
                TotalColumns = columns.Count,
                DisplayedColumns = columnStats.Count(c => !(bool)((dynamic)c).isCompletelyEmpty),
                ColumnStatistics = columnStats,
            };
        }

        private string GenerateColumnStatisticsHtml(ColumnStatisticsModel stats)
        {
            var html = new System.Text.StringBuilder();
            var hiddenColumns = stats.TotalColumns - stats.DisplayedColumns;

            html.Append("<div class=\"alert alert-info mt-3\">");
            html.Append("<h6><i class=\"fas fa-chart-bar me-2\"></i>สถิติคอลัมน์:</h6>");
            html.Append("<div class=\"row\">");
            html.Append(
                $"<div class=\"col-md-4\"><strong>คอลัมน์ทั้งหมด:</strong> {stats.TotalColumns}</div>"
            );
            html.Append(
                $"<div class=\"col-md-4\"><strong>คอลัมน์ที่แสดง:</strong> {stats.DisplayedColumns}</div>"
            );
            html.Append(
                $"<div class=\"col-md-4\"><strong>คอลัมน์ที่ซ่อน:</strong> <span class=\"badge bg-warning text-dark\">{hiddenColumns}</span></div>"
            );
            html.Append("</div>");

            if (hiddenColumns > 0)
            {
                html.Append("<hr><div class=\"mt-2\"><small class=\"text-muted\">");
                html.Append("<strong>คอลัมน์ที่ถูกซ่อน (ข้อมูลว่าง):</strong> ");

                var hiddenColumnNames = stats
                    .ColumnStatistics.Where(c => c.IsCompletelyEmpty)
                    .Select(c => $"{c.ColumnName} ({c.EmptyPercentage}% ว่าง)")
                    .ToList();

                html.Append(string.Join(", ", hiddenColumnNames));
                html.Append("</small></div>");

                var statsJson = JsonConvert
                    .SerializeObject(stats.ColumnStatistics)
                    .Replace("\"", "&quot;");
                html.Append(
                    $"<button class=\"btn btn-sm btn-outline-info mt-2 show-detailed-stats\" data-stats=\"{statsJson}\">แสดงรายละเอียดคอลัมน์</button>"
                );
            }

            html.Append("</div>");
            return html.ToString();
        }

        private string GenerateUpdatePreviewHtml(object data)
        {
            // สำหรับ demo - ในการใช้งานจริงควรมี structure ที่ชัดเจน
            return "<div class=\"alert alert-warning\">Update Preview HTML จะถูกสร้างที่นี่</div>";
        }

        private string GenerateDeletePreviewHtml(object data)
        {
            // สำหรับ demo - ในการใช้งานจริงควรมี structure ที่ชัดเจน
            return "<div class=\"alert alert-danger\">Delete Preview HTML จะถูกสร้างที่นี่</div>";
        }

        private string GenerateColumnStatsModalHtml(object data)
        {
            // สำหรับ demo - ในการใช้งานจริงควรมี structure ที่ชัดเจน
            return "<div class=\"table-responsive\">Column Stats Modal HTML จะถูกสร้างที่นี่</div>";
        }

        private object ConvertToAnonymousObject(object deserializedParams)
        {
            if (deserializedParams == null)
                return null;

            // ถ้าเป็น JObject (Newtonsoft.Json)
            if (deserializedParams is Newtonsoft.Json.Linq.JObject jObject)
            {
                var dictionary = new Dictionary<string, object>();
                foreach (var property in jObject.Properties())
                {
                    dictionary[property.Name] = property.Value?.ToObject<object>();
                }
                return dictionary;
            }

            // ถ้าเป็น Dictionary หรือ ExpandoObject ก็ return ตามเดิม
            if (
                deserializedParams is Dictionary<string, object>
                || deserializedParams is System.Dynamic.ExpandoObject
            )
            {
                return deserializedParams;
            }

            // ถ้าเป็น array หรือ list ให้ throw error
            if (
                deserializedParams is System.Collections.IEnumerable
                && !(deserializedParams is string)
            )
            {
                throw new InvalidOperationException(
                    "Array หรือ List ไม่สามารถใช้เป็น parameter ได้ กรุณาใช้ Object แทน"
                );
            }

            return deserializedParams;
        }

        // Helper methods for user information
        private string? GetCurrentUserId()
        {
            return HttpContext.Session.GetString("UserId") ?? User.Identity?.Name ?? "Anonymous";
        }

        private string? GetCurrentUserName()
        {
            return HttpContext.Session.GetString("UserName")
                ?? HttpContext.Session.GetString("FullName")
                ?? User.Identity?.Name
                ?? "Anonymous User";
        }

        private DataTable ConvertToDataTable(IEnumerable<dynamic> data)
        {
            var dataTable = new DataTable();

            if (data != null && data.Any())
            {
                var firstRow = data.First() as IDictionary<string, object>;
                if (firstRow != null)
                {
                    // เพิ่ม columns
                    foreach (var key in firstRow.Keys)
                    {
                        dataTable.Columns.Add(key, typeof(object));
                    }

                    // เพิ่ม rows
                    foreach (var item in data)
                    {
                        var row = dataTable.NewRow();
                        var dict = item as IDictionary<string, object>;
                        if (dict != null)
                        {
                            foreach (var key in dict.Keys)
                            {
                                row[key] = dict[key] ?? DBNull.Value;
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }

            return dataTable;
        }

        private List<string> GetColumnNames(DataTable dataTable)
        {
            var nonEmptyColumns = new List<string>();

            foreach (DataColumn column in dataTable.Columns)
            {
                // ตรวจสอบว่าคอลัมน์นี้มีข้อมูลที่ไม่ใช่ null หรือ empty หรือไม่
                bool hasData = false;
                foreach (DataRow row in dataTable.Rows)
                {
                    var value = row[column];
                    if (
                        value != null
                        && value != DBNull.Value
                        && !string.IsNullOrWhiteSpace(value.ToString())
                    )
                    {
                        hasData = true;
                        break;
                    }
                }

                // เพิ่มเฉพาะคอลัมน์ที่มีข้อมูล
                if (hasData)
                {
                    nonEmptyColumns.Add(column.ColumnName);
                }
            }

            return nonEmptyColumns;
        }

        private List<Dictionary<string, object>> ConvertDataTableToJson(DataTable dataTable)
        {
            var result = new List<Dictionary<string, object>>();
            var nonEmptyColumns = GetColumnNames(dataTable); // ใช้ฟังก์ชันที่กรองคอลัมน์ว่างแล้ว

            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (string columnName in nonEmptyColumns) // ใช้เฉพาะคอลัมน์ที่มีข้อมูล
                {
                    var value = row[columnName];
                    dict[columnName] = value == DBNull.Value ? null : value;
                }
                result.Add(dict);
            }

            return result;
        }

        // เพิ่มเมธอดสำหรับตรวจสอบคอลัมน์ว่าง
        private Dictionary<string, object> GetColumnStatistics(DataTable dataTable)
        {
            var stats = new Dictionary<string, object>();
            var columnStats = new List<object>();

            foreach (DataColumn column in dataTable.Columns)
            {
                int totalRows = dataTable.Rows.Count;
                int nullCount = 0;
                int emptyCount = 0;
                int validCount = 0;

                foreach (DataRow row in dataTable.Rows)
                {
                    var value = row[column];
                    if (value == null || value == DBNull.Value)
                    {
                        nullCount++;
                    }
                    else if (string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        emptyCount++;
                    }
                    else
                    {
                        validCount++;
                    }
                }

                columnStats.Add(
                    new
                    {
                        columnName = column.ColumnName,
                        totalRows = totalRows,
                        nullCount = nullCount,
                        emptyCount = emptyCount,
                        validCount = validCount,
                        emptyPercentage = totalRows > 0
                            ? Math.Round((double)(nullCount + emptyCount) / totalRows * 100, 2)
                            : 0,
                        isCompletelyEmpty = validCount == 0,
                    }
                );
            }

            stats["columnStatistics"] = columnStats;
            stats["totalColumns"] = dataTable.Columns.Count;
            stats["displayedColumns"] = GetColumnNames(dataTable).Count;

            return stats;
        }
    }
}
