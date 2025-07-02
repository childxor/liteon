using Microsoft.EntityFrameworkCore;
using IPS_TH.Models.History;

namespace IPS_TH.Data
{
    public class OracleHistoryDbContext : DbContext
    {
        public OracleHistoryDbContext(DbContextOptions<OracleHistoryDbContext> options) : base(options)
        {
        }

        public DbSet<OracleActivityHistory> OracleActivityHistory { get; set; }
        public DbSet<OracleDataChangeHistory> OracleDataChangeHistory { get; set; }
        public DbSet<OracleQueryPerformance> OracleQueryPerformance { get; set; }
        public DbSet<OracleUserSession> OracleUserSessions { get; set; }
        public DbSet<OracleSystemSetting> OracleSystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // กำหนดความสัมพันธ์ระหว่างตาราง
            modelBuilder.Entity<OracleDataChangeHistory>()
                .HasOne(d => d.ActivityHistory)
                .WithMany(a => a.DataChanges)
                .HasForeignKey(d => d.ActivityHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OracleQueryPerformance>()
                .HasOne(p => p.ActivityHistory)
                .WithMany(a => a.QueryPerformances)
                .HasForeignKey(p => p.ActivityHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // กำหนด Index สำหรับประสิทธิภาพ
            modelBuilder.Entity<OracleActivityHistory>()
                .HasIndex(a => a.SessionId);

            modelBuilder.Entity<OracleActivityHistory>()
                .HasIndex(a => a.ActivityType);

            modelBuilder.Entity<OracleActivityHistory>()
                .HasIndex(a => a.CreatedDate);

            modelBuilder.Entity<OracleActivityHistory>()
                .HasIndex(a => new { a.SessionId, a.UserId });

            modelBuilder.Entity<OracleUserSession>()
                .HasIndex(s => s.SessionId)
                .IsUnique();

            modelBuilder.Entity<OracleSystemSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // กำหนดค่าเริ่มต้นสำหรับ Settings
            modelBuilder.Entity<OracleSystemSetting>().HasData(
                new OracleSystemSetting
                {
                    Id = 1,
                    SettingKey = OracleHistoryConstants.SettingKeys.MAX_QUERY_EXECUTION_TIME_MS,
                    SettingValue = "30000",
                    SettingType = "Integer",
                    Description = "เวลาสูงสุดในการทำงานของ Query (milliseconds)",
                    IsSystem = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.Now,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.Now
                },
                new OracleSystemSetting
                {
                    Id = 2,
                    SettingKey = OracleHistoryConstants.SettingKeys.MAX_ROWS_PER_QUERY,
                    SettingValue = "10000",
                    SettingType = "Integer",
                    Description = "จำนวนแถวสูงสุดที่จะแสดงผลใน Query",
                    IsSystem = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.Now,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.Now
                },
                new OracleSystemSetting
                {
                    Id = 3,
                    SettingKey = OracleHistoryConstants.SettingKeys.LOG_RETENTION_DAYS,
                    SettingValue = "30",
                    SettingType = "Integer",
                    Description = "จำนวนวันที่เก็บประวัติการใช้งาน",
                    IsSystem = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.Now,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.Now
                },
                new OracleSystemSetting
                {
                    Id = 4,
                    SettingKey = OracleHistoryConstants.SettingKeys.ENABLE_QUERY_LOGGING,
                    SettingValue = "true",
                    SettingType = "Boolean",
                    Description = "เปิดใช้งานการบันทึกประวัติ Query",
                    IsSystem = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.Now,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.Now
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // ใช้ Connection String เดียวกับ ApplicationDbContext
                optionsBuilder.UseSqlServer("Server=ces941\\ces941;Database=HRM_IPS;User Id=IPSPGM;Password=Thips?4070!TH;TrustServerCertificate=True;");
            }
        }
    }
} 