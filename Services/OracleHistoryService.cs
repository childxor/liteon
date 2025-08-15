using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IPS_TH.Data;
using IPS_TH.Models.History;
using Microsoft.EntityFrameworkCore;

namespace IPS_TH.Services
{
    public interface IOracleHistoryService
    {
        Task<long> LogActivityAsync(LogActivityRequest request);
        Task LogDataChangesAsync(List<LogDataChangeRequest> dataChanges);
        Task<string> CreateOrUpdateSessionAsync(CreateSessionRequest request);
        Task EndSessionAsync(string sessionId);
        Task<List<OracleActivitySummaryDto>> GetActivityHistoryAsync(
            int page = 1,
            int pageSize = 50,
            string? activityType = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );
        Task<OracleActivityHistory?> GetActivityByIdAsync(long id);
        Task<List<OracleDailyActivityDto>> GetDailyActivityReportAsync(
            DateTime startDate,
            DateTime endDate
        );
        Task<List<OraclePerformanceSummaryDto>> GetPerformanceSummaryAsync(int topCount = 10);
        Task CleanupOldDataAsync(int retentionDays = 30);
        Task<Dictionary<string, object>> GetDashboardStatsAsync();
        Task<OracleSystemSetting?> GetSettingAsync(string key);
        Task<bool> SetSettingAsync(
            string key,
            string value,
            string? description = null,
            string? updatedBy = null
        );
        Task<List<OracleSystemSetting>> GetAllSettingsAsync();
    }

    public class OracleHistoryService : IOracleHistoryService
    {
        private readonly OracleHistoryDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OracleHistoryService(
            OracleHistoryDbContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<long> LogActivityAsync(LogActivityRequest request)
        {
            try
            {
                // ตรวจสอบการบันทึกซ้ำ (ภายใน 5 วินาที)
                var recentActivity = await _context
                    .OracleActivityHistory.Where(h =>
                        h.SessionId == request.SessionId
                        && h.UserId == request.UserId
                        && h.ActivityType == request.ActivityType
                        && h.SqlCommand == request.SqlCommand
                        && h.Parameters == request.Parameters
                        && h.CreatedDate >= DateTime.Now.AddSeconds(-5)
                    )
                    .FirstOrDefaultAsync();

                if (recentActivity != null)
                {
                    // พบการบันทึกซ้ำ ไม่บันทึกซ้ำ
                    return recentActivity.Id;
                }

                var activity = new OracleActivityHistory
                {
                    SessionId = request.SessionId,
                    UserId = request.UserId,
                    UserName = request.UserName,
                    ActivityType = request.ActivityType,
                    SqlCommand = request.SqlCommand,
                    Parameters = request.Parameters,
                    TableName = ExtractTableName(request.SqlCommand),
                    AffectedRows = request.AffectedRows,
                    ExecutionTimeMs = request.ExecutionTimeMs,
                    IsSuccess = request.IsSuccess,
                    ErrorMessage = request.ErrorMessage,
                    ClientIP = request.ClientIP ?? GetClientIP(),
                    UserAgent = request.UserAgent ?? GetUserAgent(),
                    CreatedDate = DateTime.Now,
                };

                _context.OracleActivityHistory.Add(activity);
                await _context.SaveChangesAsync();

                // บันทึก Performance data ถ้าเป็น SELECT query
                if (
                    request.ActivityType == OracleHistoryConstants.ActivityTypes.SELECT
                    && request.IsSuccess
                )
                {
                    await LogQueryPerformanceAsync(
                        activity.Id,
                        request.SqlCommand,
                        request.ExecutionTimeMs,
                        request.AffectedRows
                    );
                }

                // อัพเดท Session activity
                await UpdateSessionActivityAsync(request.SessionId, request.ActivityType);

                return activity.Id;
            }
            catch (Exception ex)
            {
                // Log error แต่ไม่ throw เพื่อไม่ให้กระทบกับ main functionality
                Console.WriteLine($"Error logging activity: {ex.Message}");
                return 0;
            }
        }

        public async Task LogDataChangesAsync(List<LogDataChangeRequest> dataChanges)
        {
            try
            {
                if (dataChanges?.Any() != true)
                    return;

                var changes = dataChanges
                    .Select(dc => new OracleDataChangeHistory
                    {
                        ActivityHistoryId = dc.ActivityHistoryId,
                        TableName = dc.TableName,
                        ColumnName = dc.ColumnName,
                        RecordIdentifier = dc.RecordIdentifier,
                        OldValue = dc.OldValue,
                        NewValue = dc.NewValue,
                        DataType = dc.DataType,
                        CreatedDate = DateTime.Now,
                    })
                    .ToList();

                _context.OracleDataChangeHistory.AddRange(changes);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging data changes: {ex.Message}");
            }
        }

        public async Task<string> CreateOrUpdateSessionAsync(CreateSessionRequest request)
        {
            try
            {
                var existingSession = await _context.OracleUserSessions.FirstOrDefaultAsync(s =>
                    s.SessionId == request.SessionId
                );

                if (existingSession != null)
                {
                    existingSession.LastActivity = DateTime.Now;
                    existingSession.IsActive = true;
                }
                else
                {
                    var newSession = new OracleUserSession
                    {
                        SessionId = request.SessionId,
                        UserId = request.UserId,
                        UserName = request.UserName,
                        LoginTime = DateTime.Now,
                        LastActivity = DateTime.Now,
                        IsActive = true,
                        ClientIP = request.ClientIP ?? GetClientIP(),
                        UserAgent = request.UserAgent ?? GetUserAgent(),
                    };

                    _context.OracleUserSessions.Add(newSession);
                }

                await _context.SaveChangesAsync();
                return request.SessionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error managing session: {ex.Message}");
                return request.SessionId;
            }
        }

        public async Task EndSessionAsync(string sessionId)
        {
            try
            {
                var session = await _context.OracleUserSessions.FirstOrDefaultAsync(s =>
                    s.SessionId == sessionId && s.IsActive
                );

                if (session != null)
                {
                    session.IsActive = false;
                    session.LogoutTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending session: {ex.Message}");
            }
        }

        public async Task<List<OracleActivitySummaryDto>> GetActivityHistoryAsync(
            int page = 1,
            int pageSize = 50,
            string? activityType = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        )
        {
            var query = _context.OracleActivityHistory.AsQueryable();

            if (!string.IsNullOrEmpty(activityType))
                query = query.Where(h => h.ActivityType == activityType);

            if (startDate.HasValue)
                query = query.Where(h => h.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.CreatedDate <= endDate.Value);

            var result = await query
                .OrderByDescending(h => h.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new OracleActivitySummaryDto
                {
                    Id = h.Id,
                    SessionId = h.SessionId,
                    UserId = h.UserId,
                    UserName = h.UserName,
                    ActivityType = h.ActivityType,
                    SqlCommand = h.SqlCommand,
                    Parameters = h.Parameters,
                    TableName = h.TableName,
                    AffectedRows = h.AffectedRows,
                    ExecutionTimeMs = h.ExecutionTimeMs,
                    IsSuccess = h.IsSuccess,
                    ErrorMessage = h.ErrorMessage,
                    CreatedDate = h.CreatedDate,
                    ClientIP = h.ClientIP,
                })
                .ToListAsync();

            return result;
        }

        public async Task<OracleActivityHistory?> GetActivityByIdAsync(long id)
        {
            return await _context
                .OracleActivityHistory.Include(h => h.DataChanges)
                .Include(h => h.QueryPerformances)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<List<OracleDailyActivityDto>> GetDailyActivityReportAsync(
            DateTime startDate,
            DateTime endDate
        )
        {
            var result = await _context
                .OracleActivityHistory.Where(h =>
                    h.CreatedDate.Date >= startDate.Date && h.CreatedDate.Date <= endDate.Date
                )
                .GroupBy(h => new { Date = h.CreatedDate.Date, h.ActivityType })
                .Select(g => new OracleDailyActivityDto
                {
                    ActivityDate = g.Key.Date,
                    ActivityType = g.Key.ActivityType,
                    ActivityCount = g.Count(),
                    SuccessCount = g.Count(x => x.IsSuccess),
                    ErrorCount = g.Count(x => !x.IsSuccess),
                    AvgExecutionTimeMs = g.Average(x => x.ExecutionTimeMs),
                    TotalAffectedRows = g.Sum(x => x.AffectedRows),
                })
                .OrderByDescending(x => x.ActivityDate)
                .ThenBy(x => x.ActivityType)
                .ToListAsync();

            return result;
        }

        public async Task<List<OraclePerformanceSummaryDto>> GetPerformanceSummaryAsync(
            int topCount = 10
        )
        {
            var result = await _context
                .OracleQueryPerformance.GroupBy(p => new { p.SqlHash, p.QueryType })
                .Select(g => new OraclePerformanceSummaryDto
                {
                    SqlHash = g.Key.SqlHash,
                    QueryType = g.Key.QueryType,
                    ExecutionCount = g.Count(),
                    AvgExecutionTimeMs = g.Average(x => x.ExecutionTimeMs),
                    MinExecutionTimeMs = g.Min(x => x.ExecutionTimeMs),
                    MaxExecutionTimeMs = g.Max(x => x.ExecutionTimeMs),
                    TotalRowsReturned = g.Sum(x => x.RowsReturned),
                    AvgRowsReturned = g.Average(x => x.RowsReturned),
                })
                .OrderByDescending(x => x.ExecutionCount)
                .Take(topCount)
                .ToListAsync();

            return result;
        }

        public async Task CleanupOldDataAsync(int retentionDays = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ลบ Performance data
                var performanceData = _context.OracleQueryPerformance.Where(p =>
                    p.CreatedDate < cutoffDate
                );
                _context.OracleQueryPerformance.RemoveRange(performanceData);

                // ลบ Data Change History
                var changeHistory = _context.OracleDataChangeHistory.Where(c =>
                    c.CreatedDate < cutoffDate
                );
                _context.OracleDataChangeHistory.RemoveRange(changeHistory);

                // ลบ Activity History
                var activityHistory = _context.OracleActivityHistory.Where(h =>
                    h.CreatedDate < cutoffDate
                );
                _context.OracleActivityHistory.RemoveRange(activityHistory);

                // ลบ Inactive Sessions
                var inactiveSessions = _context.OracleUserSessions.Where(s =>
                    !s.IsActive && s.LoginTime < cutoffDate
                );
                _context.OracleUserSessions.RemoveRange(inactiveSessions);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-7);
            var thisMonth = today.AddMonths(-1);

            var stats = new Dictionary<string, object>
            {
                ["TodayActivities"] = await _context.OracleActivityHistory.CountAsync(h =>
                    h.CreatedDate.Date == today
                ),
                ["WeekActivities"] = await _context.OracleActivityHistory.CountAsync(h =>
                    h.CreatedDate >= thisWeek
                ),
                ["MonthActivities"] = await _context.OracleActivityHistory.CountAsync(h =>
                    h.CreatedDate >= thisMonth
                ),
                ["TotalErrors"] = await _context.OracleActivityHistory.CountAsync(h =>
                    !h.IsSuccess
                ),
                ["ActiveSessions"] = await _context.OracleUserSessions.CountAsync(s => s.IsActive),
                ["TotalSessions"] = await _context.OracleUserSessions.CountAsync(),
                ["AvgExecutionTime"] =
                    await _context
                        .OracleActivityHistory.Where(h => h.CreatedDate >= thisWeek)
                        .AverageAsync(h => (double?)h.ExecutionTimeMs) ?? 0,
                ["TopActivity"] =
                    await _context
                        .OracleActivityHistory.Where(h => h.CreatedDate >= thisWeek)
                        .GroupBy(h => h.ActivityType)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefaultAsync() ?? "N/A",
            };

            return stats;
        }

        public async Task<OracleSystemSetting?> GetSettingAsync(string key)
        {
            return await _context.OracleSystemSettings.FirstOrDefaultAsync(s =>
                s.SettingKey == key
            );
        }

        public async Task<bool> SetSettingAsync(
            string key,
            string value,
            string? description = null,
            string? updatedBy = null
        )
        {
            try
            {
                var setting = await _context.OracleSystemSettings.FirstOrDefaultAsync(s =>
                    s.SettingKey == key
                );

                if (setting != null)
                {
                    setting.SettingValue = value;
                    setting.UpdatedBy = updatedBy;
                    setting.UpdatedDate = DateTime.Now;
                    if (!string.IsNullOrEmpty(description))
                        setting.Description = description;
                }
                else
                {
                    setting = new OracleSystemSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        Description = description,
                        CreatedBy = updatedBy,
                        CreatedDate = DateTime.Now,
                        UpdatedBy = updatedBy,
                        UpdatedDate = DateTime.Now,
                    };
                    _context.OracleSystemSettings.Add(setting);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<OracleSystemSetting>> GetAllSettingsAsync()
        {
            return await _context.OracleSystemSettings.OrderBy(s => s.SettingKey).ToListAsync();
        }

        // Private helper methods
        private async Task LogQueryPerformanceAsync(
            long activityId,
            string? sqlCommand,
            int executionTimeMs,
            int rowsReturned
        )
        {
            try
            {
                if (string.IsNullOrEmpty(sqlCommand))
                    return;

                var performance = new OracleQueryPerformance
                {
                    ActivityHistoryId = activityId,
                    SqlHash = GenerateSqlHash(sqlCommand),
                    QueryType = ExtractQueryType(sqlCommand),
                    RowsReturned = rowsReturned,
                    ExecutionTimeMs = executionTimeMs,
                    CreatedDate = DateTime.Now,
                };

                _context.OracleQueryPerformance.Add(performance);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging query performance: {ex.Message}");
            }
        }

        private async Task UpdateSessionActivityAsync(string sessionId, string activityType)
        {
            try
            {
                var session = await _context.OracleUserSessions.FirstOrDefaultAsync(s =>
                    s.SessionId == sessionId
                );

                if (session != null)
                {
                    session.LastActivity = DateTime.Now;

                    switch (activityType)
                    {
                        case OracleHistoryConstants.ActivityTypes.SELECT:
                            session.TotalQueries++;
                            break;
                        case OracleHistoryConstants.ActivityTypes.UPDATE:
                            session.TotalUpdates++;
                            break;
                        case OracleHistoryConstants.ActivityTypes.DELETE:
                            session.TotalDeletes++;
                            break;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating session activity: {ex.Message}");
            }
        }

        private string GenerateSqlHash(string sql)
        {
            // ลบ parameters และ whitespace แล้วสร้าง hash
            var normalizedSql = System.Text.RegularExpressions.Regex.Replace(
                sql.Trim().ToUpper(),
                @"\s+",
                " "
            );

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedSql));
            return Convert.ToHexString(hashBytes);
        }

        private string ExtractQueryType(string? sql)
        {
            if (string.IsNullOrEmpty(sql))
                return "UNKNOWN";

            var trimmedSql = sql.Trim().ToUpper();
            if (trimmedSql.StartsWith("SELECT"))
                return "SELECT";
            if (trimmedSql.StartsWith("INSERT"))
                return "INSERT";
            if (trimmedSql.StartsWith("UPDATE"))
                return "UPDATE";
            if (trimmedSql.StartsWith("DELETE"))
                return "DELETE";
            if (trimmedSql.StartsWith("BEGIN") || trimmedSql.StartsWith("EXEC"))
                return "PROCEDURE";

            return "OTHER";
        }

        private string? ExtractTableName(string? sql)
        {
            if (string.IsNullOrEmpty(sql))
                return null;

            try
            {
                var upperSql = sql.ToUpper();
                var fromMatch = System.Text.RegularExpressions.Regex.Match(
                    upperSql,
                    @"FROM\s+(\w+)"
                );
                if (fromMatch.Success)
                    return fromMatch.Groups[1].Value;

                var updateMatch = System.Text.RegularExpressions.Regex.Match(
                    upperSql,
                    @"UPDATE\s+(\w+)"
                );
                if (updateMatch.Success)
                    return updateMatch.Groups[1].Value;

                var insertMatch = System.Text.RegularExpressions.Regex.Match(
                    upperSql,
                    @"INTO\s+(\w+)"
                );
                if (insertMatch.Success)
                    return insertMatch.Groups[1].Value;

                var deleteMatch = System.Text.RegularExpressions.Regex.Match(
                    upperSql,
                    @"DELETE\s+FROM\s+(\w+)"
                );
                if (deleteMatch.Success)
                    return deleteMatch.Groups[1].Value;
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        private string? GetClientIP()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    return context.Connection.RemoteIpAddress?.ToString();
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        private string? GetUserAgent()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    return context.Request.Headers["User-Agent"].FirstOrDefault();
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }
    }
}
