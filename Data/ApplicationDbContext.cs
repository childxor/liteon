using System.Data;
using System.Data.SqlClient;
using Dapper;
using IPS_TH.Models; // ตรวจสอบให้แน่ใจว่ามีการนำเข้า Models
using IPS_TH.Models.Employee;
using IPS_TH.Models.AssetFactory;
using IPS_TH.Models.Attendance;
using IPS_TH.Models.ESMMS;
using Microsoft.EntityFrameworkCore;

namespace IPS_TH.Data
{
    // SQL Server DbContext (เดิม)
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Authen
        public DbSet<sys_user> sys_user { get; set; }

        public DbSet<sys_role> sys_role { get; set; }

        public DbSet<sys_role_detail> sys_role_detail { get; set; }

        public DbSet<sys_user_role> sys_user_role { get; set; }

        // Layout
        public DbSet<sys_module> sys_module { get; set; } // เพิ่ม DbSet สำหรับ sys_module

        public DbSet<sys_module_permission> sys_module_permission { get; set; } // เพิ่ม DbSet สำหรับ sys_module_permission

        public DbSet<sys_language> sys_language { get; set; } // เพิ่ม DbSet สำหรับ sys_language

        // Employee
        public DbSet<emp_person> emp_person { get; set; }

        public DbSet<emp_shift> emp_shift { get; set; }

        public DbSet<emp_person_shift> emp_person_shift { get; set; }

        // Attendance
        public DbSet<emp_wrkplan> emp_wrkplan { get; set; }
        public DbSet<TOTPlan> TOTPlan { get; set; }

        // Cabinet_emp
        public DbSet<Cabinet_emp> Cabinet_emp { get; set; }

        // LeaveOutside
        public DbSet<emp_leave_outside> emp_leave_outside { get; set; }

        // Asset
        public DbSet<Asset> Asset { get; set; }
        public DbSet<AssetMacAddress> AssetMacAddress { get; set; }
        public DbSet<AssetHistory> AssetHistory { get; set; }

        // Dept
        public DbSet<Dept> Dept { get; set; }

        // Feedback
        public DbSet<sys_user_feedback> sys_user_feedback { get; set; }

        public DbSet<sys_user_feedback_comment> sys_user_feedback_comment { get; set; }

        public DbSet<sys_user_feedback_like> sys_user_feedback_like { get; set; }

        // E-SMMS
        public DbSet<esmms_course> esmms_course { get; set; }
        public DbSet<esmms_exam> esmms_exam { get; set; }
        public DbSet<esmms_exam_employee> esmms_exam_employee { get; set; }
        public DbSet<esmms_question> esmms_question { get; set; }
        public DbSet<esmms_exam_detail> esmms_exam_detail { get; set; }
        public DbSet<esmms_employees_receive_mail> esmms_employees_receive_mail { get; set; }
    }

    // Oracle DbContext (สำหรับเชื่อมต่อ Oracle Database)
    public class OracleDbContext : DbContext
    {
        public OracleDbContext(DbContextOptions<OracleDbContext> options)
            : base(options) { }

        // เพิ่ม DbSet สำหรับตารางใน Oracle ตามความต้องการ
        // ตัวอย่าง:
        // public DbSet<OracleTable> OracleTable { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // กำหนดการตั้งค่าเฉพาะสำหรับ Oracle เช่น Schema, Table names
            base.OnModelCreating(modelBuilder);
        }
    }
}
