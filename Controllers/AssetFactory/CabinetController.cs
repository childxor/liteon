using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
// เพิ่ม using สำหรับการทำงานกับการสร้าง PDF
using System.Text;
using System.Threading.Tasks;
using Dapper;
// เพิ่ม using สำหรับ DinkToPdf
using DinkToPdf;
using DinkToPdf.Contracts;
using IPS_TH.Data;
using IPS_TH.Models.Employee;
using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Employee
{
    public class CabinetController : BaseController
    {
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _ces941ConnectionString;
        private readonly IConverter _converter;
        private readonly IViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public CabinetController(
            IConfiguration configuration,
            ApplicationDbContext context,
            IConverter converter = null,
            IViewEngine viewEngine = null,
            ITempDataProvider tempDataProvider = null
        )
            : base(context)
        {
            _dbContext = context;
            _connectionString = context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = configuration.GetConnectionString("CES941");
            _converter = converter;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        // Method สำหรับดึงข้อมูลทั้งหมดสำหรับรายงาน
        private async Task<List<dynamic>> GetAllForReport()
        {
            var result = await GetAll();
            if (result is JsonResult jsonResult && jsonResult.Value != null)
            {
                // แปลง JsonResult เป็น List<dynamic>
                var dynamicList =
                    jsonResult.Value as System.Collections.Generic.IEnumerable<dynamic>;
                if (dynamicList != null)
                {
                    return dynamicList.ToList();
                }
            }
            return new List<dynamic>();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                await LoadPermissions("Cabinet", "Index");
                return View("~/Views/AssetFactory/Cabinet.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Cabinet action: {ex.Message}");
                return View("~/Views/AssetFactory/Cabinet.cshtml");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // เปิดการเชื่อมต่อกับฐานข้อมูล CES941 เพื่อดึงข้อมูลพนักงาน
                    using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                    {
                        await ces941Connection.OpenAsync();

                        // ดึงข้อมูลตู้เก็บของ
                        var query =
                            @"SELECT Id, No, Owner, status, updated_at, updated_by, created_at, created_by 
                                     FROM Cabinet_emp 
                                     WHERE record_status = 'N' 
                                     ORDER BY No";
                        var cabinets = (await connection.QueryAsync<Cabinet_emp>(query)).ToList();

                        // สร้างรายการรหัสพนักงานที่เป็นเจ้าของตู้
                        var ownerIds = cabinets
                            .Where(c => !string.IsNullOrEmpty(c.Owner))
                            .Select(c => c.Owner)
                            .Distinct()
                            .ToList();

                        // ถ้ามีเจ้าของตู้ ให้ดึงข้อมูลพนักงานจาก CES941
                        if (ownerIds.Any())
                        {
                            // สร้าง SQL parameter สำหรับ IN clause
                            string paramNames = string.Join(
                                ",",
                                ownerIds.Select((_, i) => $"@p{i}")
                            );
                            var employeeQuery =
                                $@"SELECT 
                                                CAST(EmpNo AS VARCHAR) as EmpNo, 
                                                CONCAT(
                                                    RTRIM(PrefixName), 
                                                    ' ', 
                                                    RTRIM(EmpName), 
                                                    ' ', 
                                                    RTRIM(EmpLName)
                                                ) as FullName,
                                                CASE 
                                                    WHEN EmpResignDate IS NOT NULL THEN 1
                                                    ELSE 0
                                                END as IsResigned,
                                                CONVERT(VARCHAR(10), EmpResignDate, 103) as ResignDate
                                              FROM MEmpBasic 
                                              WHERE CAST(EmpNo AS VARCHAR) IN ({paramNames})";

                            // สร้าง parameters dictionary
                            var parameters = new DynamicParameters();
                            for (int i = 0; i < ownerIds.Count; i++)
                            {
                                parameters.Add($"p{i}", ownerIds[i]);
                            }

                            // ดึงข้อมูลพนักงาน
                            var employeesQuery = await ces941Connection.QueryAsync<dynamic>(
                                employeeQuery,
                                parameters
                            );

                            // ตรวจสอบการซ้ำของ EmpNo และเลือกข้อมูลล่าสุด
                            var employees = employeesQuery
                                .GroupBy(e => (string)e.EmpNo)
                                .ToDictionary(
                                    g => g.Key,
                                    g =>
                                    {
                                        var employee = g.First(); // เลือกข้อมูลแรกในกรณีที่มีซ้ำ
                                        return new
                                        {
                                            FullName = (string)employee.FullName,
                                            IsResigned = (int)employee.IsResigned == 1,
                                            ResignDate = (string)employee.ResignDate,
                                        };
                                    }
                                );

                            // เพิ่มการล็อกเพื่อตรวจสอบข้อมูล
                            Console.WriteLine(
                                $"Found {employeesQuery.Count()} employee records, {employees.Count} unique employees"
                            );

                            // เพิ่มชื่อพนักงานใน response
                            var result = cabinets.Select(c => new
                            {
                                c.Id,
                                c.No,
                                c.Owner,
                                OwnerName = !string.IsNullOrEmpty(c.Owner)
                                && employees.ContainsKey(c.Owner)
                                    ? employees[c.Owner].FullName
                                    : null,
                                IsResigned = !string.IsNullOrEmpty(c.Owner)
                                && employees.ContainsKey(c.Owner)
                                    ? employees[c.Owner].IsResigned
                                    : false,
                                ResignDate = !string.IsNullOrEmpty(c.Owner)
                                && employees.ContainsKey(c.Owner)
                                    ? employees[c.Owner].ResignDate
                                    : null,
                                c.status,
                                c.updated_at,
                                c.updated_by,
                                c.created_at,
                                c.created_by,
                            });

                            return Json(result);
                        }

                        // ถ้าไม่มีเจ้าของตู้ ส่งข้อมูลตู้เก็บของตามปกติ
                        return Json(cabinets);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAll error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query =
                        @"SELECT Id, No, Owner, status, updated_at, updated_by, created_at, created_by 
                                 FROM Cabinet_emp 
                                 WHERE Id = @Id AND record_status = 'N'";
                    var cabinet = await connection.QueryFirstOrDefaultAsync<Cabinet_emp>(
                        query,
                        new { Id = id }
                    );

                    if (cabinet == null)
                    {
                        return NotFound("ไม่พบข้อมูลตู้เก็บของ");
                    }

                    // ถ้ามีผู้ครอบครอง ดึงข้อมูลเพิ่มเติมจาก CES941
                    if (!string.IsNullOrEmpty(cabinet.Owner))
                    {
                        try
                        {
                            using (
                                var ces941Connection = new SqlConnection(_ces941ConnectionString)
                            )
                            {
                                await ces941Connection.OpenAsync();
                                var employeeQuery =
                                    @"SELECT 
                                                 CAST(EmpNo AS VARCHAR) as EmpNo,
                                                 CONCAT(
                                                     RTRIM(PrefixName), 
                                                     ' ', 
                                                     RTRIM(EmpName), 
                                                     ' ', 
                                                     RTRIM(EmpLName)
                                                 ) as FullName,
                                                 CASE 
                                                     WHEN EmpResignDate IS NOT NULL THEN 1
                                                     ELSE 0
                                                 END as IsResigned,
                                                 CONVERT(VARCHAR(10), EmpResignDate, 103) as ResignDate
                                               FROM MEmpBasic 
                                               WHERE CAST(EmpNo AS VARCHAR) = @EmpNo";

                                var employeeList = await ces941Connection.QueryAsync<dynamic>(
                                    employeeQuery,
                                    new { EmpNo = cabinet.Owner }
                                );

                                // กรณีที่มีข้อมูลพนักงานหลายคน ให้เลือกคนแรก
                                var employee = employeeList.FirstOrDefault();

                                var result = new
                                {
                                    cabinet.Id,
                                    cabinet.No,
                                    cabinet.Owner,
                                    OwnerName = employee != null ? (string)employee.FullName : null,
                                    IsResigned = employee != null
                                        ? (int)employee.IsResigned == 1
                                        : false,
                                    ResignDate = employee != null
                                        ? (string)employee.ResignDate
                                        : null,
                                    cabinet.status,
                                    cabinet.updated_at,
                                    cabinet.updated_by,
                                    cabinet.created_at,
                                    cabinet.created_by,
                                };

                                return Json(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting employee data: {ex.Message}");
                            // แม้จะเกิดข้อผิดพลาดในการดึงข้อมูลพนักงาน ก็ยังส่งข้อมูลตู้คืน
                        }
                    }

                    return Json(cabinet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cabinet_emp cabinet)
        {
            try
            {
                // บันทึกข้อมูลที่ได้รับเข้ามาเพื่อตรวจสอบ
                Console.WriteLine($"ข้อมูลที่ได้รับ: {JsonConvert.SerializeObject(cabinet)}");

                if (cabinet == null)
                {
                    Console.WriteLine("ข้อมูลที่ได้รับเป็น null");
                    return BadRequest("ข้อมูลไม่ถูกต้อง (cabinet เป็น null)");
                }

                if (string.IsNullOrEmpty(cabinet.No))
                {
                    Console.WriteLine("ไม่ได้ระบุเลขที่ตู้");
                    return BadRequest("กรุณาระบุเลขที่ตู้");
                }

                // ถ้าสถานะเป็น "ว่าง" (0) ให้ล้างข้อมูลผู้ครอบครอง
                if (cabinet.status == "0")
                {
                    cabinet.Owner = "";
                }

                // ถ้าสถานะเป็น "มีผู้ใช้งาน" (1) แต่ไม่ได้ระบุผู้ครอบครอง
                if (cabinet.status == "1" && string.IsNullOrEmpty(cabinet.Owner))
                {
                    return BadRequest("สถานะ 'มีผู้ใช้งาน' ต้องระบุข้อมูลผู้ครอบครอง");
                }

                // ตรวจสอบเลขที่ตู้ซ้ำ
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var checkQuery =
                        "SELECT COUNT(1) FROM Cabinet_emp WHERE No = @No AND record_status = 'N'";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { No = cabinet.No }
                    );

                    if (exists > 0)
                    {
                        Console.WriteLine($"เลขที่ตู้ {cabinet.No} มีอยู่ในระบบแล้ว");
                        return BadRequest("เลขที่ตู้นี้มีอยู่ในระบบแล้ว");
                    }

                    // เพิ่มข้อมูลการสร้าง
                    cabinet.created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cabinet.created_by = User?.Identity?.Name ?? "system";
                    cabinet.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cabinet.updated_by = User?.Identity?.Name ?? "system";
                    // กำหนดค่าเริ่มต้นถ้าไม่ได้ระบุมา
                    cabinet.status = string.IsNullOrEmpty(cabinet.status) ? "0" : cabinet.status;
                    cabinet.record_status = "N";

                    var insertQuery =
                        @"INSERT INTO Cabinet_emp (No, Owner, status, created_at, created_by, updated_at, updated_by, record_status) 
                                       VALUES (@No, @Owner, @status, @created_at, @created_by, @updated_at, @updated_by, @record_status);
                                       SELECT CAST(SCOPE_IDENTITY() as int)";

                    Console.WriteLine($"กำลังบันทึกข้อมูล: {JsonConvert.SerializeObject(cabinet)}");
                    var id = await connection.ExecuteScalarAsync<int>(insertQuery, cabinet);
                    Console.WriteLine($"บันทึกสำเร็จ id: {id}");

                    return Ok(new { Id = id, Message = "บันทึกข้อมูลสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดข้อผิดพลาด: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Cabinet_emp cabinet)
        {
            try
            {
                if (cabinet == null)
                {
                    return BadRequest("ข้อมูลไม่ถูกต้อง");
                }

                if (cabinet.Id <= 0)
                {
                    return BadRequest("รหัสตู้ไม่ถูกต้อง");
                }

                if (string.IsNullOrEmpty(cabinet.No))
                {
                    return BadRequest("กรุณาระบุเลขที่ตู้");
                }

                // ถ้าสถานะเป็น "ว่าง" (0) ให้ล้างข้อมูลผู้ครอบครอง
                if (cabinet.status == "0")
                {
                    cabinet.Owner = "";
                }

                // ถ้าสถานะเป็น "มีผู้ใช้งาน" (1) แต่ไม่ได้ระบุผู้ครอบครอง
                if (cabinet.status == "1" && string.IsNullOrEmpty(cabinet.Owner))
                {
                    return BadRequest("สถานะ 'มีผู้ใช้งาน' ต้องระบุข้อมูลผู้ครอบครอง");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบเลขที่ตู้ซ้ำ
                    var checkQuery =
                        "SELECT COUNT(1) FROM Cabinet_emp WHERE No = @No AND Id != @Id AND record_status = 'N'";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { No = cabinet.No, Id = cabinet.Id }
                    );

                    if (exists > 0)
                    {
                        return BadRequest("เลขที่ตู้นี้มีอยู่ในระบบแล้ว");
                    }

                    // อัปเดตข้อมูล
                    cabinet.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cabinet.updated_by = User?.Identity?.Name ?? "system";

                    var updateQuery =
                        @"UPDATE Cabinet_emp 
                                       SET No = @No, 
                                           Owner = @Owner, 
                                           status = @status, 
                                           updated_at = @updated_at, 
                                           updated_by = @updated_by 
                                       WHERE Id = @Id";

                    var affected = await connection.ExecuteAsync(updateQuery, cabinet);

                    if (affected == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้เก็บของที่ต้องการแก้ไข");
                    }

                    return Ok(new { Message = "อัปเดตข้อมูลสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpDelete]
        [Route("Cabinet_emp/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // เช็คว่ามีข้อมูลที่จะลบหรือไม่
                    var checkQuery =
                        "SELECT COUNT(1) FROM Cabinet_emp WHERE Id = @Id AND record_status = 'N'";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { Id = id }
                    );

                    if (exists == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้เก็บของที่ต้องการลบ");
                    }

                    // ทำ Soft Delete โดยการอัปเดต record_status เป็น 'D'
                    var deleteQuery =
                        @"UPDATE Cabinet_emp 
                                       SET record_status = 'D', 
                                           updated_at = @updated_at, 
                                           updated_by = @updated_by 
                                       WHERE Id = @Id";

                    await connection.ExecuteAsync(
                        deleteQuery,
                        new
                        {
                            Id = id,
                            updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            updated_by = User?.Identity?.Name ?? "system",
                        }
                    );

                    return Ok(new { Message = "ลบข้อมูลสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var query =
                        @"SELECT 
                                    CAST(EmpNo AS VARCHAR) as id, 
                                    CONCAT(
                                        RTRIM(PrefixName), 
                                        ' ', 
                                        RTRIM(EmpName), 
                                        ' ', 
                                        RTRIM(EmpLName), 
                                        ' (', 
                                        CAST(EmpNo AS VARCHAR), 
                                        ')'
                                    ) as fullName 
                                 FROM MEmpBasic 
                                 WHERE Language = 'EN'
                                 ORDER BY EmpName, EmpLName";

                    var employees = await connection.QueryAsync(query);
                    return Json(employees);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetEmployees error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByOwner(string owner)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query =
                        @"SELECT Id, No, Owner, status, updated_at, updated_by, created_at, created_by
                                 FROM Cabinet_emp
                                 WHERE Owner = @Owner AND record_status = 'N'";

                    var cabinets = await connection.QueryAsync<Cabinet_emp>(
                        query,
                        new { Owner = owner }
                    );
                    return Json(cabinets);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailable()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // เปิดการเชื่อมต่อกับฐานข้อมูล CES941 เพื่อดึงข้อมูลพนักงาน
                    using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                    {
                        await ces941Connection.OpenAsync();

                        // ดึงข้อมูลตู้ทั้งหมด (ว่าง, ชำรุด, และมีผู้ใช้งาน)
                        var query =
                            @"SELECT Id, No, Owner, status, updated_at, updated_by, created_at, created_by 
                                     FROM Cabinet_emp 
                                     WHERE record_status = 'N' 
                                     ORDER BY 
                                         CASE 
                                             WHEN status = '0' THEN 1  -- ว่าง (แสดงก่อน)
                                             WHEN status = '2' THEN 2  -- ชำรุด
                                             WHEN status = '1' THEN 3  -- มีผู้ใช้งาน (แสดงหลัง)
                                             ELSE 4
                                         END,
                                         CAST(No AS INT)";

                        var cabinets = (await connection.QueryAsync<Cabinet_emp>(query)).ToList();

                        // สร้างรายการรหัสพนักงานที่เป็นเจ้าของตู้
                        var ownerIds = cabinets
                            .Where(c => !string.IsNullOrEmpty(c.Owner))
                            .Select(c => c.Owner)
                            .Distinct()
                            .ToList();

                        // ถ้ามีเจ้าของตู้ ให้ดึงข้อมูลพนักงานจาก CES941
                        Dictionary<string, dynamic> employees = new Dictionary<string, dynamic>();
                        if (ownerIds.Any())
                        {
                            // สร้าง SQL parameter สำหรับ IN clause
                            string paramNames = string.Join(
                                ",",
                                ownerIds.Select((_, i) => $"@p{i}")
                            );
                            var employeeQuery =
                                $@"SELECT 
                                                CAST(EmpNo AS VARCHAR) as EmpNo, 
                                                CONCAT(
                                                    RTRIM(PrefixName), 
                                                    ' ', 
                                                    RTRIM(EmpName), 
                                                    ' ', 
                                                    RTRIM(EmpLName)
                                                ) as FullName,
                                                CASE 
                                                    WHEN EmpResignDate IS NOT NULL THEN 1
                                                    ELSE 0
                                                END as IsResigned
                                              FROM MEmpBasic 
                                              WHERE CAST(EmpNo AS VARCHAR) IN ({paramNames})";

                            // สร้าง parameters dictionary
                            var parameters = new DynamicParameters();
                            for (int i = 0; i < ownerIds.Count; i++)
                            {
                                parameters.Add($"p{i}", ownerIds[i]);
                            }

                            // ดึงข้อมูลพนักงาน
                            var employeesQuery = await ces941Connection.QueryAsync<dynamic>(
                                employeeQuery,
                                parameters
                            );

                            employees = employeesQuery
                                .GroupBy(e => (string)e.EmpNo)
                                .ToDictionary(g => g.Key, g => g.First());
                        }

                        // เพิ่มข้อมูลพนักงานใน response
                        var result = cabinets.Select(c => new
                        {
                            c.Id,
                            c.No,
                            c.Owner,
                            OwnerName = !string.IsNullOrEmpty(c.Owner)
                            && employees.ContainsKey(c.Owner)
                                ? (string)employees[c.Owner].FullName
                                : null,
                            IsResigned = !string.IsNullOrEmpty(c.Owner)
                            && employees.ContainsKey(c.Owner)
                                ? (int)employees[c.Owner].IsResigned == 1
                                : false,
                            c.status,
                            c.updated_at,
                            c.updated_by,
                            c.created_at,
                            c.created_by,
                        });

                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAvailable error: {ex.Message}");
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignToEmployee([FromBody] Cabinet_emp cabinet)
        {
            try
            {
                if (cabinet == null || cabinet.Id <= 0)
                {
                    return BadRequest("ข้อมูลไม่ถูกต้อง");
                }

                if (string.IsNullOrEmpty(cabinet.Owner))
                {
                    return BadRequest("กรุณาระบุรหัสพนักงาน");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่าตู้มีอยู่จริงหรือไม่
                    var checkQuery =
                        "SELECT COUNT(1) FROM Cabinet_emp WHERE Id = @Id AND record_status = 'N'";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { Id = cabinet.Id }
                    );

                    if (exists == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้ที่ต้องการกำหนด");
                    }

                    // อัปเดตข้อมูล
                    cabinet.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cabinet.updated_by = User?.Identity?.Name ?? "system";
                    cabinet.status = "1"; // สถานะ "มีผู้ใช้งาน"

                    var updateQuery =
                        @"UPDATE Cabinet_emp 
                                       SET Owner = @Owner, 
                                           status = @status,
                                           updated_at = @updated_at, 
                                           updated_by = @updated_by 
                                       WHERE Id = @Id";

                    var affected = await connection.ExecuteAsync(updateQuery, cabinet);

                    if (affected == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้ที่ต้องการกำหนด");
                    }

                    return Ok(new { Message = "กำหนดตู้ให้พนักงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReleaseCabinet([FromBody] Cabinet_emp cabinet)
        {
            try
            {
                if (cabinet == null || cabinet.Id <= 0)
                {
                    return BadRequest("ข้อมูลไม่ถูกต้อง");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่าตู้มีอยู่จริงหรือไม่
                    var checkQuery =
                        "SELECT COUNT(1) FROM Cabinet_emp WHERE Id = @Id AND record_status = 'N'";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { Id = cabinet.Id }
                    );

                    if (exists == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้ที่ต้องการคืน");
                    }

                    // อัปเดตข้อมูล
                    var updateData = new
                    {
                        Id = cabinet.Id,
                        Owner = "", // ล้างข้อมูลผู้ครอบครอง
                        status = "0", // สถานะ "ว่าง"
                        updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        updated_by = User?.Identity?.Name ?? "system",
                    };

                    var updateQuery =
                        @"UPDATE Cabinet_emp 
                                       SET Owner = @Owner, 
                                           status = @status,
                                           updated_at = @updated_at, 
                                           updated_by = @updated_by 
                                       WHERE Id = @Id";

                    var affected = await connection.ExecuteAsync(updateQuery, updateData);

                    if (affected == 0)
                    {
                        return NotFound("ไม่พบข้อมูลตู้ที่ต้องการคืน");
                    }

                    return Ok(new { Message = "คืนตู้สำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("~/api/Cabinet/ReleaseResignedOwners")]
        public async Task<IActionResult> ReleaseResignedOwners()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    await ces941Connection.OpenAsync();

                    // ดึงรายการตู้ที่มีผู้ครอบครองอยู่
                    var cabinetList = (await connection.QueryAsync<dynamic>(
                        @"SELECT Id, No, Owner, status
                          FROM Cabinet_emp
                          WHERE record_status = 'N' AND ISNULL(Owner,'') <> ''"
                    )).ToList();

                    if (!cabinetList.Any())
                    {
                        return Ok(new { Affected = 0, Message = "ไม่มีตู้ที่มีผู้ครอบครอง" });
                    }

                    var ownerIds = cabinetList
                        .Select(c => (string)c.Owner)
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .Distinct()
                        .ToList();

                    if (!ownerIds.Any())
                    {
                        return Ok(new { Affected = 0, Message = "ไม่มีผู้ครอบครองให้ตรวจสอบ" });
                    }

                    // ตรวจสอบพนักงานที่ลาออกจาก CES941
                    string resignParamNames = string.Join(
                        ",",
                        ownerIds.Select((_, i) => $"@p{i}")
                    );

                    var resignParams = new DynamicParameters();
                    for (int i = 0; i < ownerIds.Count; i++)
                    {
                        resignParams.Add($"p{i}", ownerIds[i]);
                    }

                    var resignedOwners = (await ces941Connection.QueryAsync<string>(
                        $@"SELECT CAST(EmpNo AS VARCHAR)
                           FROM MEmpBasic
                           WHERE CAST(EmpNo AS VARCHAR) IN ({resignParamNames})
                             AND EmpResignDate IS NOT NULL",
                        resignParams
                    )).ToList();

                    if (!resignedOwners.Any())
                    {
                        return Ok(new { Affected = 0, Message = "ไม่พบเจ้าของตู้ที่ลาออกแล้ว" });
                    }

                    // อัปเดตตู้เฉพาะที่สถานะเป็นมีผู้ใช้งาน (1) และเจ้าของลาออกแล้ว
                    var updateParams = new DynamicParameters();
                    for (int i = 0; i < resignedOwners.Count; i++)
                    {
                        updateParams.Add($"r{i}", resignedOwners[i]);
                    }
                    updateParams.Add("updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    updateParams.Add("updated_by", User?.Identity?.Name ?? "system");

                    string inClause = string.Join(
                        ",",
                        resignedOwners.Select((_, i) => $"@r{i}")
                    );

                    var affected = await connection.ExecuteAsync(
                        $@"UPDATE Cabinet_emp
                           SET Owner = '',
                               status = '0',
                               updated_at = @updated_at,
                               updated_by = @updated_by
                         WHERE record_status = 'N'
                           AND status = '1'
                           AND ISNULL(Owner,'') <> ''
                           AND CAST(Owner AS VARCHAR) IN ({inClause})",
                        updateParams
                    );

                    return Ok(new { Affected = affected, Message = affected > 0 ? "คืนตู้สำเร็จ" : "ไม่มีรายการที่ต้องคืน" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("~/api/Cabinet/BatchCreate")]
        public async Task<IActionResult> BatchCreateCabinets([FromBody] CabinetRangeModel model)
        {
            try
            {
                Console.WriteLine(
                    $"BatchCreateCabinets ได้รับข้อมูล: {JsonConvert.SerializeObject(model)}"
                );

                if (model == null || string.IsNullOrEmpty(model.Range))
                {
                    return BadRequest("กรุณาระบุช่วงของตู้ที่ต้องการเพิ่ม");
                }

                string range = model.Range;

                // แยกช่วงหมายเลขตู้ เช่น "6-100"
                string[] rangeParts = range.Split('-');
                if (rangeParts.Length != 2)
                {
                    return BadRequest(
                        "รูปแบบช่วงไม่ถูกต้อง ต้องเป็นรูปแบบ 'เลขเริ่มต้น-เลขสิ้นสุด' เช่น '6-100'"
                    );
                }

                // แปลงเป็นตัวเลข
                if (
                    !int.TryParse(rangeParts[0], out int startNo)
                    || !int.TryParse(rangeParts[1], out int endNo)
                )
                {
                    return BadRequest("หมายเลขตู้ต้องเป็นตัวเลขเท่านั้น");
                }

                // ตรวจสอบว่าช่วงถูกต้อง
                if (startNo > endNo)
                {
                    return BadRequest("หมายเลขเริ่มต้นต้องน้อยกว่าหรือเท่ากับหมายเลขสิ้นสุด");
                }

                // เตรียมข้อมูลผลลัพธ์
                var result = new
                {
                    TotalRequested = endNo - startNo + 1,
                    Created = new List<int>(),
                    Skipped = new List<int>(),
                    Failed = new List<KeyValuePair<int, string>>(),
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ดึงหมายเลขตู้ที่มีอยู่แล้วในระบบ
                    var existingQuery = "SELECT No FROM Cabinet_emp WHERE record_status = 'N'";
                    var existingCabinets = (
                        await connection.QueryAsync<string>(existingQuery)
                    ).ToHashSet();

                    // สร้างตู้ทีละหมายเลขในช่วงที่กำหนด
                    for (int i = startNo; i <= endNo; i++)
                    {
                        string cabinetNo = i.ToString();

                        // ข้ามหมายเลขที่มีอยู่แล้ว
                        if (existingCabinets.Contains(cabinetNo))
                        {
                            result.Skipped.Add(i);
                            continue;
                        }

                        try
                        {
                            // สร้างข้อมูลตู้ใหม่
                            var newCabinet = new Cabinet_emp
                            {
                                No = cabinetNo,
                                Owner = "",
                                status = "0", // ว่าง
                                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                created_by = User?.Identity?.Name ?? "system",
                                updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                updated_by = User?.Identity?.Name ?? "system",
                                record_status = "N",
                            };

                            // เพิ่มข้อมูลตู้ใหม่
                            var insertQuery =
                                @"INSERT INTO Cabinet_emp (No, Owner, status, created_at, created_by, updated_at, updated_by, record_status) 
                                               VALUES (@No, @Owner, @status, @created_at, @created_by, @updated_at, @updated_by, @record_status);
                                               SELECT CAST(SCOPE_IDENTITY() as int)";

                            var id = await connection.ExecuteScalarAsync<int>(
                                insertQuery,
                                newCabinet
                            );
                            result.Created.Add(i);
                        }
                        catch (Exception ex)
                        {
                            // บันทึกกรณีเกิดข้อผิดพลาด
                            result.Failed.Add(new KeyValuePair<int, string>(i, ex.Message));
                        }
                    }
                }

                // สรุปผลการดำเนินการ
                var summary = new
                {
                    Range = range,
                    TotalRequested = result.TotalRequested,
                    TotalCreated = result.Created.Count,
                    TotalSkipped = result.Skipped.Count,
                    TotalFailed = result.Failed.Count,
                    CreatedCabinets = result.Created,
                    SkippedCabinets = result.Skipped,
                    FailedCabinets = result.Failed,
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BatchCreateCabinets error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCabinetStats()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ดึงสถิติการใช้งานตู้เก็บของ
                    var statsQuery =
                        @"
                        SELECT 
                            COUNT(CASE WHEN status = '0' THEN 1 END) as AvailableCabinets,
                            COUNT(CASE WHEN status = '1' THEN 1 END) as OccupiedCabinets,
                            COUNT(CASE WHEN status = '2' THEN 1 END) as DamagedCabinets,
                            COUNT(*) as TotalCabinets
                        FROM Cabinet_emp 
                        WHERE record_status = 'N'";

                    var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(statsQuery);

                    // ตรวจสอบว่า stats ไม่เป็น null
                    if (stats == null)
                    {
                        return Json(
                            new
                            {
                                TotalCabinets = 0,
                                AvailableCabinets = 0,
                                OccupiedCabinets = 0,
                                DamagedCabinets = 0,
                                ResignedOwnedCabinets = 0,
                                EmployeesWithoutCabinet = 0,
                            }
                        );
                    }

                    int resignedOwnedCabinets = 0;
                    int employeesWithoutCabinet = 0;

                    // ตรวจสอบการเชื่อมต่อ CES941 และดึงข้อมูลพนักงานลาออก
                    try
                    {
                        if (!string.IsNullOrEmpty(_ces941ConnectionString))
                        {
                            using (
                                var ces941Connection = new SqlConnection(_ces941ConnectionString)
                            )
                            {
                                await ces941Connection.OpenAsync();

                                // ดึงรายการตู้ที่มีผู้ครอบครอง
                                var occupiedCabinetsQuery =
                                    @"
                                    SELECT Owner 
                                    FROM Cabinet_emp 
                                    WHERE record_status = 'N' 
                                    AND status = '1' 
                                    AND Owner IS NOT NULL 
                                    AND Owner != ''";

                                var occupiedOwners = (
                                    await connection.QueryAsync<string>(occupiedCabinetsQuery)
                                )?.ToList();

                                // ดึงรายการพนักงานทั้งหมดที่ยังไม่ลาออก
                                var activeEmployeesQuery =
                                    @"
                                    SELECT DISTINCT CAST(EmpNo AS VARCHAR) as EmpNo 
                                    FROM MEmpBasic 
                                    WHERE EmpResignDate IS NULL
                                    AND Language = 'EN'";

                                var activeEmployeesList = await ces941Connection.QueryAsync<string>(
                                    activeEmployeesQuery
                                );

                                // นับพนักงานที่ยังไม่มีตู้
                                if (activeEmployeesList != null && occupiedOwners != null)
                                {
                                    var uniqueOccupiedOwners = occupiedOwners.Distinct().ToList();
                                    employeesWithoutCabinet = activeEmployeesList.Count(emp =>
                                        !uniqueOccupiedOwners.Contains(emp)
                                    );
                                }

                                if (occupiedOwners != null && occupiedOwners.Any())
                                {
                                    // ใช้ DISTINCT เพื่อนับพนักงานที่ unique เท่านั้น
                                    // และนับจำนวนตู้ที่ต้องคืน ไม่ใช่จำนวนพนักงาน

                                    // สร้าง SQL parameter สำหรับ IN clause
                                    string paramNames = string.Join(
                                        ",",
                                        occupiedOwners.Select((_, i) => $"@p{i}")
                                    );

                                    // นับจำนวนตู้ที่เจ้าของลาออกแล้ว (ไม่ใช่นับจำนวนพนักงาน)
                                    var resignedOwnersQuery =
                                        $@"
                                        SELECT DISTINCT CAST(EmpNo AS VARCHAR) as EmpNo
                                        FROM MEmpBasic 
                                        WHERE CAST(EmpNo AS VARCHAR) IN ({paramNames})
                                        AND EmpResignDate IS NOT NULL";

                                    // สร้าง parameters dictionary
                                    var parameters = new DynamicParameters();
                                    for (int i = 0; i < occupiedOwners.Count; i++)
                                    {
                                        parameters.Add($"p{i}", occupiedOwners[i]);
                                    }

                                    var resignedOwnersList =
                                        await ces941Connection.QueryAsync<string>(
                                            resignedOwnersQuery,
                                            parameters
                                        );

                                    // นับจำนวนตู้ที่เจ้าของลาออกแล้ว
                                    resignedOwnedCabinets = occupiedOwners.Count(owner =>
                                        resignedOwnersList.Contains(owner)
                                    );
                                }

                                System.Diagnostics.Debug.WriteLine(
                                    $"Active employees: {activeEmployeesList?.Count() ?? 0}"
                                );
                                System.Diagnostics.Debug.WriteLine(
                                    $"Employees without cabinet: {employeesWithoutCabinet}"
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking resigned employees: {ex.Message}");
                        // ถ้าเกิดข้อผิดพลาดในการตรวจสอบพนักงานลาออก ให้ใช้ค่า 0
                        resignedOwnedCabinets = 0;
                        employeesWithoutCabinet = 0;
                    }

                    return Json(
                        new
                        {
                            TotalCabinets = stats.TotalCabinets != null
                                ? (int)stats.TotalCabinets
                                : 0,
                            AvailableCabinets = stats.AvailableCabinets != null
                                ? (int)stats.AvailableCabinets
                                : 0,
                            OccupiedCabinets = stats.OccupiedCabinets != null
                                ? (int)stats.OccupiedCabinets
                                : 0,
                            DamagedCabinets = stats.DamagedCabinets != null
                                ? (int)stats.DamagedCabinets
                                : 0,
                            ResignedOwnedCabinets = resignedOwnedCabinets,
                            EmployeesWithoutCabinet = employeesWithoutCabinet,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCabinetStats error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // ส่งค่าเริ่มต้นกลับไปแทนการ error
                return Json(
                    new
                    {
                        TotalCabinets = 0,
                        AvailableCabinets = 0,
                        OccupiedCabinets = 0,
                        DamagedCabinets = 0,
                        ResignedOwnedCabinets = 0,
                        EmployeesWithoutCabinet = 0,
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> Report()
        {
            try
            {
                // ดึงข้อมูลเหมือนกับ GetAll
                var cabinets = await GetAll();

                // ใช้ path แบบเต็ม
                return View("~/Views/AssetFactory/report/report_cabinet_list.cshtml", cabinets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาดในการแสดงรายงาน: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPDFWithiText()
        {
            try
            {
                var cabinetsResult = await GetAllForReport();
                if (cabinetsResult == null)
                {
                    return BadRequest("ไม่สามารถดึงข้อมูลตู้ได้");
                }

                // เรียกใช้เมธอด GenerateHtmlForPDF ที่ใช้ Razor View เพื่อแสดงผล
                string html = await GenerateHtmlForPDF(cabinetsResult);

                if (string.IsNullOrEmpty(html))
                {
                    return StatusCode(500, "ไม่สามารถสร้าง HTML สำหรับรายงานได้");
                }

                // สร้าง PDF โดยใช้ iTextSharp
                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        // สร้าง PDF Writer
                        var writer = new PdfWriter(ms);
                        var pdf = new PdfDocument(writer);
                        var document = new iText.Layout.Document(
                            pdf,
                            iText.Kernel.Geom.PageSize.A4
                        );

                        // กำหนด default font
                        HtmlConverter.ConvertToPdf(html, pdf, new ConverterProperties());

                        // ปิด document
                        document.Close();

                        // ส่งเป็นไฟล์ PDF
                        string fileName = $"cabinet_report_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                        return File(ms.ToArray(), "application/pdf", fileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating PDF with iText: {ex.Message}");
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่สามารถสร้าง PDF ได้: " + ex.Message,
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExportPDFWithiText: {ex.Message}");
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportHtml()
        {
            try
            {
                // ดึงข้อมูลตู้
                var cabinetsResult = await GetAllForReport();
                if (cabinetsResult == null)
                {
                    return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลตู้ได้" });
                }

                // ดึงข้อมูลสถิติ
                var stats = await GetCabinetStatsData();

                // ใช้ RazorEngine เพื่อแสดงผล template เป็น HTML
                var viewModel = cabinetsResult;
                var viewBag = new Dictionary<string, object>
                {
                    { "TotalCabinets", stats.TotalCabinets },
                    { "AvailableCabinets", stats.AvailableCabinets },
                    { "OccupiedCabinets", stats.OccupiedCabinets },
                    { "DamagedCabinets", stats.DamagedCabinets },
                    { "ResignedOwnedCabinets", stats.ResignedOwnedCabinets },
                    { "EmployeesWithoutCabinet", stats.EmployeesWithoutCabinet },
                    { "GeneratedAt", DateTime.Now },
                };

                // เรียกใช้ service เพื่อแสดงผล template
                string htmlContent = await RenderViewToStringAsync(
                    "report_cabinet_list",
                    viewModel,
                    viewBag
                );

                // ส่งค่ากลับเป็น JSON
                return Json(
                    new
                    {
                        success = true,
                        htmlContent = htmlContent,
                        totalRecords = cabinetsResult.Count,
                        generatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateHtmlReport(object cabinetData)
        {
            try
            {
                // ตรวจสอบว่ามีไฟล์ template หรือไม่
                string templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "report",
                    "report_cabinet_list.cshtml"
                );

                string htmlContent;

                if (System.IO.File.Exists(templatePath))
                {
                    // อ่านไฟล์ template
                    htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);

                    // แปลงข้อมูลเป็น List<dynamic>
                    var cabinets =
                        cabinetData as List<dynamic>
                        ?? (cabinetData as IEnumerable<dynamic>)?.ToList()
                        ?? new List<dynamic>();

                    // ดึงสถิติ
                    var stats = await GetCabinetStatsData();

                    // แทนที่ placeholders ในไฟล์ template
                    htmlContent = await ProcessTemplate(htmlContent, cabinets, stats);
                }
                else
                {
                    // ถ้าไม่มีไฟล์ template ให้สร้าง HTML แบบ inline
                    Console.WriteLine($"Template file not found at: {templatePath}");
                    htmlContent = await GenerateInlineHtmlReport(cabinetData);
                }

                return htmlContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateHtmlReport: {ex.Message}");
                // ถ้าเกิดข้อผิดพลาด ให้สร้าง HTML แบบ fallback
                return await GenerateInlineHtmlReport(cabinetData);
            }
        }

        // Method สำหรับประมวลผล template
        private async Task<string> ProcessTemplate(
            string template,
            List<dynamic> cabinets,
            dynamic stats
        )
        {
            try
            {
                // แทนที่ข้อมูลสถิติ
                template = template
                    .Replace("{{TOTAL_CABINETS}}", stats.TotalCabinets.ToString())
                    .Replace("{{AVAILABLE_CABINETS}}", stats.AvailableCabinets.ToString())
                    .Replace("{{OCCUPIED_CABINETS}}", stats.OccupiedCabinets.ToString())
                    .Replace("{{DAMAGED_CABINETS}}", stats.DamagedCabinets.ToString())
                    .Replace("{{RESIGNED_OWNED_CABINETS}}", stats.ResignedOwnedCabinets.ToString())
                    .Replace(
                        "{{EMPLOYEES_WITHOUT_CABINET}}",
                        stats.EmployeesWithoutCabinet.ToString()
                    )
                    .Replace("{{REPORT_DATE}}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
                    .Replace("{{REPORT_TITLE}}", "รายงานข้อมูลตู้เก็บของพนักงาน");

                // สร้างตารางข้อมูล
                var tableRows = new StringBuilder();
                int rowNumber = 1;

                foreach (var cabinet in cabinets)
                {
                    string statusText,
                        statusClass,
                        icon;
                    switch (cabinet.status?.ToString())
                    {
                        case "0":
                            statusText = "ว่าง";
                            statusClass = "badge-success";
                            icon = "check-circle";
                            break;
                        case "1":
                            statusText = "มีผู้ใช้งาน";
                            statusClass = "badge-primary";
                            icon = "user-check";
                            break;
                        case "2":
                            statusText = "ชำรุด";
                            statusClass = "badge-danger";
                            icon = "exclamation-circle";
                            break;
                        default:
                            statusText = "ไม่ระบุ";
                            statusClass = "badge-secondary";
                            icon = "question-circle";
                            break;
                    }

                    string ownerDisplay = "-";
                    if (!string.IsNullOrEmpty(cabinet.OwnerName))
                    {
                        ownerDisplay = cabinet.OwnerName;
                        if (cabinet.IsResigned == true)
                        {
                            ownerDisplay +=
                                " <span class='badge badge-danger ms-1'>ลาออกแล้ว</span>";
                        }
                    }
                    else if (!string.IsNullOrEmpty(cabinet.Owner))
                    {
                        ownerDisplay = cabinet.Owner;
                    }

                    tableRows.AppendLine(
                        $@"
                        <tr>
                            <td class='text-center'>{rowNumber}</td>
                            <td class='text-center fw-bold'>{cabinet.No}</td>
                            <td>{ownerDisplay}</td>
                            <td class='text-center'>
                                <span class='badge {statusClass}'>
                                    <i class='fas fa-{icon} me-1'></i>{statusText}
                                </span>
                            </td>
                            <td class='text-center'>{cabinet.updated_at ?? "-"}</td>
                            <td class='text-center'>{cabinet.updated_by ?? "-"}</td>
                        </tr>"
                    );

                    rowNumber++;
                }

                // แทนที่ตารางข้อมูล
                template = template.Replace("{{TABLE_ROWS}}", tableRows.ToString());
                template = template.Replace("{{TOTAL_RECORDS}}", cabinets.Count.ToString());

                return template;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing template: {ex.Message}");
                return await GenerateInlineHtmlReport(cabinets);
            }
        }

        // Method สำหรับสร้าง HTML แบบ inline (fallback)
        private async Task<string> GenerateInlineHtmlReport(object cabinetData)
        {
            // ตรวจสอบว่า cabinetData ไม่เป็น null
            if (cabinetData == null)
            {
                return "<div class='alert alert-danger'>ไม่พบข้อมูลตู้เก็บของ</div>";
            }

            // แปลง dynamic เป็น List<dynamic>
            var cabinets = cabinetData as List<dynamic>;
            if (cabinets == null)
            {
                try
                {
                    cabinets = new List<dynamic>((IEnumerable<dynamic>)cabinetData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting data: {ex.Message}");
                    return "<div class='alert alert-danger'>เกิดข้อผิดพลาดในการแปลงข้อมูล</div>";
                }
            }

            // คำนวณสถิติข้อมูล
            var stats = new
            {
                Total = cabinets.Count,
                Available = cabinets.Count(c => c.status?.ToString() == "0"),
                Occupied = cabinets.Count(c => c.status?.ToString() == "1"),
                Damaged = cabinets.Count(c => c.status?.ToString() == "2"),
                Resigned = cabinets.Count(c => c.status?.ToString() == "1" && c.IsResigned == true),
            };

            var html =
                $@"
    <div class='container-fluid'>
        <div class='row g-3 mb-4'>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-light'>
                        <h3 class='text-secondary mb-1'>{stats.Total}</h3>
                        <small class='text-muted'>ตู้ทั้งหมด</small>
                    </div>
                </div>
            </div>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-success text-white'>
                        <h3 class='mb-1'>{stats.Available}</h3>
                        <small>ตู้ว่าง</small>
                    </div>
                </div>
            </div>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-primary text-white'>
                        <h3 class='mb-1'>{stats.Occupied}</h3>
                        <small>มีผู้ใช้งาน</small>
                    </div>
                </div>
            </div>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-danger text-white'>
                        <h3 class='mb-1'>{stats.Damaged}</h3>
                        <small>ชำรุด</small>
                    </div>
                </div>
            </div>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-warning text-white'>
                        <h3 class='mb-1'>{stats.Resigned}</h3>
                        <small>เจ้าของลาออก</small>
                    </div>
                </div>
            </div>
            <div class='col-md-2'>
                <div class='card'>
                    <div class='card-body text-center bg-info text-white'>
                        <h3 class='mb-1'>-</h3>
                        <small>พนักงานไม่มีตู้</small>
                    </div>
                </div>
            </div>
        </div>

        <div class='card'>
            <div class='card-header bg-dark text-white'>
                <h5 class='mb-0'><i class='fas fa-list me-2'></i>รายละเอียดตู้เก็บของ ({cabinets.Count} รายการ)</h5>
            </div>
            <div class='card-body p-0'>
                <div class='table-responsive'>
                    <table class='table table-striped table-hover mb-0'>
                        <thead class='table-dark'>
                            <tr>
                                <th class='text-center' style='width: 60px;'>#</th>
                                <th class='text-center' style='width: 100px;'>เลขที่ตู้</th>
                                <th>ผู้ครอบครอง</th>
                                <th class='text-center' style='width: 130px;'>สถานะ</th>
                                <th class='text-center' style='width: 160px;'>อัพเดทล่าสุด</th>
                                <th class='text-center' style='width: 120px;'>อัพเดทโดย</th>
                            </tr>
                        </thead>
                        <tbody>";

            int rowNumber = 1;
            foreach (var cabinet in cabinets)
            {
                string statusText,
                    statusClass,
                    icon;
                switch (cabinet.status?.ToString())
                {
                    case "0":
                        statusText = "ว่าง";
                        statusClass = "bg-success";
                        icon = "check-circle";
                        break;
                    case "1":
                        statusText = "มีผู้ใช้งาน";
                        statusClass = "bg-primary";
                        icon = "user-check";
                        break;
                    case "2":
                        statusText = "ชำรุด";
                        statusClass = "bg-danger";
                        icon = "exclamation-circle";
                        break;
                    default:
                        statusText = "ไม่ระบุ";
                        statusClass = "bg-secondary";
                        icon = "question-circle";
                        break;
                }

                string ownerDisplay = "<span class='text-muted'>-</span>";
                if (!string.IsNullOrEmpty(cabinet.OwnerName))
                {
                    ownerDisplay = cabinet.OwnerName;
                    if (cabinet.IsResigned == true)
                    {
                        ownerDisplay +=
                            $@" <span class='badge bg-danger ms-1' title='วันที่ลาออก: {cabinet.ResignDate ?? "ไม่ระบุ"}'>ลาออกแล้ว</span>";
                    }
                }
                else if (!string.IsNullOrEmpty(cabinet.Owner))
                {
                    ownerDisplay = $"<span class='text-primary'>{cabinet.Owner}</span>";
                }

                html +=
                    $@"
                            <tr>
                                <td class='text-center'>{rowNumber}</td>
                                <td class='text-center fw-bold'>{cabinet.No}</td>
                                <td>{ownerDisplay}</td>
                                <td class='text-center'>
                                    <span class='badge {statusClass}'>
                                        <i class='fas fa-{icon} me-1'></i>{statusText}
                                    </span>
                                </td>
                                <td class='text-center'>{cabinet.updated_at ?? "-"}</td>
                                <td class='text-center'>{cabinet.updated_by ?? "-"}</td>
                            </tr>";
                rowNumber++;
            }

            html +=
                @"
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        
        <div class='mt-3 text-center text-muted'>
            <small>
                <i class='fas fa-info-circle me-1'></i>
                ทั้งหมด {cabinets.Count} รายการ | อัพเดทล่าสุด: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
            </small>
        </div>
    </div>";

            return html;
        }

        // เพิ่ม method สำหรับตรวจสอบสถานะ PDF service
        [HttpGet]
        [Route("Cabinet/CheckPDFStatus")]
        public IActionResult CheckPDFStatus()
        {
            try
            {
                // ตรวจสอบ iText7
                bool iTextAvailable = true;
                try
                {
                    using var testStream = new MemoryStream();
                    var testWriter = new PdfWriter(testStream);
                    var testPdf = new PdfDocument(testWriter);
                    testPdf.Close();
                }
                catch
                {
                    iTextAvailable = false;
                }

                return Json(
                    new
                    {
                        success = true,
                        pdfServiceAvailable = iTextAvailable,
                        pdfEngine = "iText7",
                        dinkToPdfAvailable = _converter != null,
                        iText7Available = iTextAvailable,
                        message = iTextAvailable
                            ? "PDF service พร้อมใช้งาน (iText7)"
                            : "PDF service ไม่พร้อมใช้งาน",
                        recommendedAction = iTextAvailable ? "ใช้ iText7" : "ใช้ HTML Fallback",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        pdfServiceAvailable = false,
                        message = $"เกิดข้อผิดพลาด: {ex.Message}",
                    }
                );
            }
        }

        // เพิ่มเมธอด RenderViewToStringAsync เพื่อแสดงผล Razor View เป็น string
        private async Task<string> RenderViewToStringAsync(
            string viewName,
            object model,
            Dictionary<string, object> viewBag = null
        )
        {
            try
            {
                // เนื่องจากไฟล์ report_cabinet_list.cshtml อยู่ใน wwwroot/report
                // เราจึงอ่านไฟล์โดยตรงและแทนที่ค่าตัวแปรในเทมเพลต
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(), 
                    "wwwroot", 
                    "report", 
                    $"{viewName}.cshtml"
                );
                
                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Template file not found: {filePath}");
                }
                
                // อ่านเนื้อหาของไฟล์
                string template = await System.IO.File.ReadAllTextAsync(filePath);
                
                // แทนที่ค่าตัวแปร ViewBag
                if (viewBag != null)
                {
                    foreach (var item in viewBag)
                    {
                        template = template.Replace($"@ViewBag.{item.Key}", item.Value?.ToString() ?? "");
                    }
                }
                
                // สร้าง HTML โดยการแทนที่ @Model
                // สำหรับ List<dynamic> จะต้องใช้ Razor Template Engine จริงๆ ซึ่งซับซ้อนเกินกว่าจะทำเองได้ง่ายๆ
                // ดังนั้นเราจะใช้วิธีการอ่านไฟล์และแทนที่ค่าตัวแปรพื้นฐานเท่านั้น
                
                // แทนที่ @DateTime.Now ด้วยเวลาปัจจุบัน
                template = template.Replace("@DateTime.Now.ToString(\"dd/MM/yyyy HH:mm:ss\")", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                template = template.Replace("@DateTime.Now", DateTime.Now.ToString());
                
                // แทนที่ค่าตัวแปรสถิติ
                template = template.Replace("@Model?.Count", ((model as List<dynamic>)?.Count ?? 0).ToString());
                template = template.Replace("@ViewBag.TotalCabinets", viewBag?["TotalCabinets"]?.ToString() ?? "0");
                template = template.Replace("@ViewBag.AvailableCabinets", viewBag?["AvailableCabinets"]?.ToString() ?? "0");
                template = template.Replace("@ViewBag.OccupiedCabinets", viewBag?["OccupiedCabinets"]?.ToString() ?? "0");
                template = template.Replace("@ViewBag.DamagedCabinets", viewBag?["DamagedCabinets"]?.ToString() ?? "0");
                template = template.Replace("@ViewBag.ResignedOwnedCabinets", viewBag?["ResignedOwnedCabinets"]?.ToString() ?? "0");
                template = template.Replace("@ViewBag.EmployeesWithoutCabinet", viewBag?["EmployeesWithoutCabinet"]?.ToString() ?? "0");
                
                // สร้าง HTML สำหรับตาราง (ส่วนข้อมูลตู้)
                var cabinetData = model as List<dynamic>;
                if (cabinetData != null && cabinetData.Any())
                {
                    // สร้าง HTML สำหรับแถวข้อมูล
                    var tableRows = new StringBuilder();
                    int rowNumber = 1;
                    
                    foreach (var cabinet in cabinetData)
                    {
                        string statusText = "ไม่ระบุ";
                        string statusBadge = "badge-secondary";
                        
                        switch (cabinet.status?.ToString())
                        {
                            case "0":
                                statusText = "ว่าง";
                                statusBadge = "badge-success";
                                break;
                            case "1":
                                statusText = "มีผู้ใช้งาน";
                                statusBadge = "badge-primary";
                                break;
                            case "2":
                                statusText = "ชำรุด";
                                statusBadge = "badge-danger";
                                break;
                        }
                        
                        string ownerDisplay = "<span class='text-muted'>-</span>";
                        if (!string.IsNullOrEmpty(cabinet.OwnerName))
                        {
                            ownerDisplay = cabinet.OwnerName;
                            if (cabinet.IsResigned == true)
                            {
                                string resignDate = cabinet.ResignDate ?? "ไม่ระบุ";
                                ownerDisplay += $@" <span class='badge bg-danger ms-1' title='วันที่ลาออก: {resignDate}'>ลาออกแล้ว</span>";
                            }
                        }
                        else if (!string.IsNullOrEmpty(cabinet.Owner))
                        {
                            ownerDisplay = $"<span class='text-primary'>{cabinet.Owner}</span>";
                        }
                        
                        tableRows.AppendLine($@"
                        <tr>
                            <td class='text-center'>{rowNumber}</td>
                            <td class='text-center text-nowrap'><strong>{cabinet.No}</strong></td>
                            <td>{ownerDisplay}</td>
                            <td class='text-center'>
                                <span class='badge {statusBadge}'>{statusText}</span>
                            </td>
                            <td class='text-center text-nowrap'>{cabinet.updated_at ?? "-"}</td>
                            <td class='text-center'>{cabinet.updated_by ?? "-"}</td>
                        </tr>");
                        
                        rowNumber++;
                    }
                    
                    // แทนที่ส่วนของข้อมูลในตาราง
                    int startIndex = template.IndexOf("@{");
                    int endIndex = template.IndexOf("</tbody>");
                    
                    if (startIndex != -1 && endIndex != -1)
                    {
                        // ค้นหาตำแหน่งเริ่มต้นของ <tbody>
                        int tbodyStartIndex = template.LastIndexOf("<tbody>", endIndex);
                        if (tbodyStartIndex != -1)
                        {
                            // แทนที่ส่วนระหว่าง <tbody> และ </tbody>
                            string beforeTbody = template.Substring(0, tbodyStartIndex + 7); // +7 for "<tbody>"
                            string afterTbodyEnd = template.Substring(endIndex);
                            
                            template = beforeTbody + tableRows.ToString() + afterTbodyEnd;
                        }
                    }
                }
                
                // ลบโค้ด Razor ที่เหลือ
                template = template.Replace("@{", "<!-- ");
                template = template.Replace("}", " -->");
                
                return template;
            }
            catch (Exception ex)
            {
                // ในกรณีที่เกิดข้อผิดพลาด ให้สร้าง HTML เริ่มต้น
                Console.WriteLine($"Error rendering template: {ex.Message}");
                return $"<div class='alert alert-danger'>เกิดข้อผิดพลาดในการสร้างรายงาน: {ex.Message}</div>";
            }
        }

        // เพิ่ม method สำหรับดาวน์โหลด PDF แยกต่างหาก
        [HttpGet]
        [Route("Cabinet/DownloadPDF")]
        public async Task<IActionResult> DownloadPDF()
        {
            try
            {
                if (_converter == null)
                {
                    return BadRequest("DinkToPdf ยังไม่ได้รับการลงทะเบียน");
                }

                var cabinetsResult = await GetAllForReport();
                if (cabinetsResult == null)
                {
                    return BadRequest("ไม่สามารถดึงข้อมูลตู้ได้");
                }

                string html = await GenerateHtmlForPDF(cabinetsResult);

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings
                    {
                        Top = 15,
                        Bottom = 15,
                        Left = 10,
                        Right = 10,
                    },
                    DocumentTitle = "รายงานข้อมูลตู้เก็บของพนักงาน",
                };

                var objectSettings = new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8", PrintMediaType = true },
                    HeaderSettings =
                    {
                        FontName = "Sarabun",
                        FontSize = 9,
                        Right = "หน้า [page] จาก [toPage]",
                        Line = true,
                    },
                    FooterSettings =
                    {
                        FontName = "Sarabun",
                        FontSize = 8,
                        Line = true,
                        Center = "รายงานข้อมูลตู้เก็บของพนักงาน",
                    },
                };

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings },
                };

                var file = _converter.Convert(pdf);
                string fileName = $"cabinet_report_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                // ส่งเป็นไฟล์ดาวน์โหลด
                return File(file, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DownloadPDF: {ex.Message}");
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        // เพิ่ม method ดึงสถิติข้อมูล
        private async Task<dynamic> GetCabinetStatsData()
        {
            try 
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var statsQuery =
                        @"
                SELECT 
                    COUNT(CASE WHEN status = '0' THEN 1 END) as AvailableCabinets,
                    COUNT(CASE WHEN status = '1' THEN 1 END) as OccupiedCabinets,
                    COUNT(CASE WHEN status = '2' THEN 1 END) as DamagedCabinets,
                    COUNT(*) as TotalCabinets
                FROM Cabinet_emp 
                WHERE record_status = 'N'";

                    var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(statsQuery);

                    int resignedOwnedCabinets = 0;
                    int employeesWithoutCabinet = 0;

                    // ตรวจสอบการเชื่อมต่อ CES941 และดึงข้อมูลพนักงานลาออก
                    try
                    {
                        if (!string.IsNullOrEmpty(_ces941ConnectionString))
                        {
                            using (
                                var ces941Connection = new SqlConnection(_ces941ConnectionString)
                            )
                            {
                                await ces941Connection.OpenAsync();

                                // ดึงรายการตู้ที่มีผู้ครอบครอง
                                var occupiedCabinetsQuery =
                                    @"
                                    SELECT Owner 
                                    FROM Cabinet_emp 
                                    WHERE record_status = 'N' 
                                    AND status = '1' 
                                    AND Owner IS NOT NULL 
                                    AND Owner != ''";

                                var occupiedOwners = (
                                    await connection.QueryAsync<string>(occupiedCabinetsQuery)
                                )?.ToList();

                                // ดึงรายการพนักงานทั้งหมดที่ยังไม่ลาออก
                                var activeEmployeesQuery =
                                    @"
                                    SELECT DISTINCT CAST(EmpNo AS VARCHAR) as EmpNo 
                                    FROM MEmpBasic 
                                    WHERE EmpResignDate IS NULL
                                    AND Language = 'EN'";

                                var activeEmployeesList = await ces941Connection.QueryAsync<string>(
                                    activeEmployeesQuery
                                );

                                // นับพนักงานที่ยังไม่มีตู้
                                if (activeEmployeesList != null && occupiedOwners != null)
                                {
                                    var uniqueOccupiedOwners = occupiedOwners.Distinct().ToList();
                                    employeesWithoutCabinet = activeEmployeesList.Count(emp =>
                                        !uniqueOccupiedOwners.Contains(emp)
                                    );
                                }

                                if (occupiedOwners != null && occupiedOwners.Any())
                                {
                                    // ใช้ DISTINCT เพื่อนับพนักงานที่ unique เท่านั้น
                                    // และนับจำนวนตู้ที่ต้องคืน ไม่ใช่จำนวนพนักงาน

                                    // สร้าง SQL parameter สำหรับ IN clause
                                    string paramNames = string.Join(
                                        ",",
                                        occupiedOwners.Select((_, i) => $"@p{i}")
                                    );

                                    // นับจำนวนตู้ที่เจ้าของลาออกแล้ว (ไม่ใช่นับจำนวนพนักงาน)
                                    var resignedOwnersQuery =
                                        $@"
                                        SELECT DISTINCT CAST(EmpNo AS VARCHAR) as EmpNo
                                        FROM MEmpBasic 
                                        WHERE CAST(EmpNo AS VARCHAR) IN ({paramNames})
                                        AND EmpResignDate IS NOT NULL";

                                    // สร้าง parameters dictionary
                                    var parameters = new DynamicParameters();
                                    for (int i = 0; i < occupiedOwners.Count; i++)
                                    {
                                        parameters.Add($"p{i}", occupiedOwners[i]);
                                    }

                                    var resignedOwnersList =
                                        await ces941Connection.QueryAsync<string>(
                                            resignedOwnersQuery,
                                            parameters
                                        );

                                    // นับจำนวนตู้ที่เจ้าของลาออกแล้ว
                                    resignedOwnedCabinets = occupiedOwners.Count(owner =>
                                        resignedOwnersList.Contains(owner)
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking resigned employees: {ex.Message}");
                        // ถ้าเกิดข้อผิดพลาดในการตรวจสอบพนักงานลาออก ให้ใช้ค่า 0
                        resignedOwnedCabinets = 0;
                        employeesWithoutCabinet = 0;
                    }

                    return new
                    {
                        TotalCabinets = stats?.TotalCabinets ?? 0,
                        AvailableCabinets = stats.AvailableCabinets != null
                            ? (int)stats.AvailableCabinets
                            : 0,
                        OccupiedCabinets = stats.OccupiedCabinets != null
                            ? (int)stats.OccupiedCabinets
                            : 0,
                        DamagedCabinets = stats.DamagedCabinets != null
                            ? (int)stats.DamagedCabinets
                            : 0,
                        ResignedOwnedCabinets = resignedOwnedCabinets,
                        EmployeesWithoutCabinet = employeesWithoutCabinet,
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCabinetStatsData error: {ex.Message}");
                return new
                {
                    TotalCabinets = 0,
                    AvailableCabinets = 0,
                    OccupiedCabinets = 0,
                    DamagedCabinets = 0,
                    ResignedOwnedCabinets = 0,
                    EmployeesWithoutCabinet = 0,
                };
            }
        }

        // เพิ่มคลาสสำหรับรับข้อมูลช่วงตู้
        public class CabinetRangeModel
        {
            /// <summary>
            /// ช่วงของหมายเลขตู้ที่ต้องการเพิ่ม เช่น "6-100"
            /// </summary>
            public string Range { get; set; }
        }

        // Method ใหม่สำหรับสร้าง HTML ที่เหมาะกับ PDF
        private async Task<string> GenerateHtmlForPDF(List<dynamic> cabinetData)
        {
            var stats = await GetCabinetStatsData();
            
            // ใช้ RazorEngine เพื่อแสดงผล template โดยตรง
            var viewModel = cabinetData;
            var viewBag = new Dictionary<string, object>
            {
                { "TotalCabinets", stats.TotalCabinets },
                { "AvailableCabinets", stats.AvailableCabinets },
                { "OccupiedCabinets", stats.OccupiedCabinets },
                { "DamagedCabinets", stats.DamagedCabinets },
                { "ResignedOwnedCabinets", stats.ResignedOwnedCabinets },
                { "EmployeesWithoutCabinet", stats.EmployeesWithoutCabinet },
                { "GeneratedAt", DateTime.Now },
            };
            
            // เรียกใช้ service เพื่อแสดงผล template
            // ปรับเส้นทางของ view ให้ถูกต้องตามโครงสร้างของ ASP.NET Core
            string renderedHtml = await RenderViewToStringAsync(
                "report_cabinet_list",
                viewModel,
                viewBag
            );
            return renderedHtml;
        }
    }
}
