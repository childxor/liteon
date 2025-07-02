using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPS_TH.Models.History
{
    // 1. ตารางหลักสำหรับเก็บ Oracle Activity History
    [Table("OracleActivityHistory")]
    public class OracleActivityHistory
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SessionId { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(200)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; }

        public string? SqlCommand { get; set; }

        public string? Parameters { get; set; }

        [MaxLength(200)]
        public string? TableName { get; set; }

        public int AffectedRows { get; set; } = 0;

        public int ExecutionTimeMs { get; set; } = 0;

        public bool IsSuccess { get; set; } = true;

        public string? ErrorMessage { get; set; }

        [MaxLength(50)]
        public string? ClientIP { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<OracleDataChangeHistory> DataChanges { get; set; } = new List<OracleDataChangeHistory>();
        public virtual ICollection<OracleQueryPerformance> QueryPerformances { get; set; } = new List<OracleQueryPerformance>();
    }

    // 2. ตารางสำหรับเก็บรายละเอียดการเปลี่ยนแปลงข้อมูล
    [Table("OracleDataChangeHistory")]
    public class OracleDataChangeHistory
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ActivityHistoryId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TableName { get; set; }

        [Required]
        [MaxLength(200)]
        public string ColumnName { get; set; }

        [MaxLength(500)]
        public string? RecordIdentifier { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [MaxLength(50)]
        public string? DataType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ActivityHistoryId")]
        public virtual OracleActivityHistory ActivityHistory { get; set; }
    }

    // 3. ตารางสำหรับเก็บ Query Performance
    [Table("OracleQueryPerformance")]
    public class OracleQueryPerformance
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ActivityHistoryId { get; set; }

        [MaxLength(64)]
        public string? SqlHash { get; set; }

        [MaxLength(50)]
        public string? QueryType { get; set; }

        public int RowsReturned { get; set; } = 0;

        public int ExecutionTimeMs { get; set; }

        public int MemoryUsedKB { get; set; } = 0;

        public bool CacheHit { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ActivityHistoryId")]
        public virtual OracleActivityHistory ActivityHistory { get; set; }
    }

    // 4. ตารางสำหรับเก็บข้อมูล Login/Session
    [Table("OracleUserSessions")]
    public class OracleUserSession
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SessionId { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(200)]
        public string? UserName { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime LastActivity { get; set; } = DateTime.Now;

        public DateTime? LogoutTime { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? ClientIP { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int TotalQueries { get; set; } = 0;

        public int TotalUpdates { get; set; } = 0;

        public int TotalDeletes { get; set; } = 0;
    }

    // 5. ตารางสำหรับเก็บ System Configuration และ Settings
    [Table("OracleSystemSettings")]
    public class OracleSystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; }

        public string? SettingValue { get; set; }

        [MaxLength(50)]
        public string SettingType { get; set; } = "String";

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsSystem { get; set; } = false;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }

    // DTO Classes สำหรับ Views และ Reporting
    public class OracleActivitySummaryDto
    {
        public long Id { get; set; }
        public string SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActivityType { get; set; }
        public string? SqlCommand { get; set; }
        public string? Parameters { get; set; }
        public string? TableName { get; set; }
        public int AffectedRows { get; set; }
        public int ExecutionTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LoginTime { get; set; }
        public string? ClientIP { get; set; }
    }

    public class OraclePerformanceSummaryDto
    {
        public string? SqlHash { get; set; }
        public string? QueryType { get; set; }
        public int ExecutionCount { get; set; }
        public double AvgExecutionTimeMs { get; set; }
        public int MinExecutionTimeMs { get; set; }
        public int MaxExecutionTimeMs { get; set; }
        public long TotalRowsReturned { get; set; }
        public double AvgRowsReturned { get; set; }
    }

    public class OracleDailyActivityDto
    {
        public DateTime ActivityDate { get; set; }
        public string ActivityType { get; set; }
        public int ActivityCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double AvgExecutionTimeMs { get; set; }
        public long TotalAffectedRows { get; set; }
    }

    // Request/Response DTOs
    public class LogActivityRequest
    {
        public string SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActivityType { get; set; }
        public string? SqlCommand { get; set; }
        public string? Parameters { get; set; }
        public string? TableName { get; set; }
        public int AffectedRows { get; set; } = 0;
        public int ExecutionTimeMs { get; set; } = 0;
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? ClientIP { get; set; }
        public string? UserAgent { get; set; }
    }

    public class LogDataChangeRequest
    {
        public long ActivityHistoryId { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string? RecordIdentifier { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? DataType { get; set; }
    }

    public class CreateSessionRequest
    {
        public string SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ClientIP { get; set; }
        public string? UserAgent { get; set; }
    }

    public class ActivityHistoryResponse
    {
        public long Id { get; set; }
        public string SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActivityType { get; set; }
        public string? TableName { get; set; }
        public int AffectedRows { get; set; }
        public int ExecutionTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<DataChangeResponse> DataChanges { get; set; } = new();
    }

    public class DataChangeResponse
    {
        public long Id { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string? RecordIdentifier { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? DataType { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Models สำหรับ Column Statistics
    public class ColumnStatisticsModel
    {
        public int TotalColumns { get; set; }
        public int DisplayedColumns { get; set; }
        public List<ColumnStatistic> ColumnStatistics { get; set; } = new();
    }

    public class ColumnStatistic
    {
        public string ColumnName { get; set; }
        public int TotalRows { get; set; }
        public int NullCount { get; set; }
        public int EmptyCount { get; set; }
        public int ValidCount { get; set; }
        public double EmptyPercentage { get; set; }
        public bool IsCompletelyEmpty { get; set; }
    }

    // Enums
    public enum ActivityType
    {
        SELECT,
        INSERT,
        UPDATE,
        DELETE,
        CONNECTION_TEST,
        PROCEDURE_CALL,
        SETTINGS_UPDATE,
        SETTINGS_DELETE,
        LOGIN,
        LOGOUT
    }

    public enum SettingType
    {
        String,
        Integer,
        Boolean,
        JSON,
        Decimal
    }

    // Constants
    public static class OracleHistoryConstants
    {
        public const int DEFAULT_PAGE_SIZE = 50;
        public const int MAX_PAGE_SIZE = 1000;
        public const int DEFAULT_RETENTION_DAYS = 30;
        public const int MAX_SQL_DISPLAY_LENGTH = 500;
        public const int MAX_ERROR_MESSAGE_LENGTH = 2000;
        
        public static class ActivityTypes
        {
            public const string SELECT = "SELECT";
            public const string INSERT = "INSERT";
            public const string UPDATE = "UPDATE";
            public const string DELETE = "DELETE";
            public const string CONNECTION_TEST = "CONNECTION_TEST";
            public const string PROCEDURE_CALL = "PROCEDURE_CALL";
            public const string SETTINGS_UPDATE = "SETTINGS_UPDATE";
            public const string SETTINGS_DELETE = "SETTINGS_DELETE";
            public const string LOGIN = "LOGIN";
            public const string LOGOUT = "LOGOUT";
        }

        public static class SettingKeys
        {
            public const string MAX_QUERY_EXECUTION_TIME_MS = "MaxQueryExecutionTimeMs";
            public const string MAX_ROWS_PER_QUERY = "MaxRowsPerQuery";
            public const string LOG_RETENTION_DAYS = "LogRetentionDays";
            public const string ENABLE_QUERY_LOGGING = "EnableQueryLogging";
            public const string ENABLE_PERFORMANCE_LOGGING = "EnablePerformanceLogging";
            public const string ALLOWED_TABLES = "AllowedTables";
        }
    }
} 