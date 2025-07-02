using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Employee
{
    public class DepartmentsController : BaseController
    {
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _ces941ConnectionString;

        public DepartmentsController(IConfiguration configuration, ApplicationDbContext context)
            : base(context)
        {
            _dbContext = context;
            _connectionString = context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = configuration.GetConnectionString("CES941");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                LoadPermissions("Departments", "Index");
                return View("~/Views/Employee/DepartmentList.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Departments action: {ex.Message}");
                return View("~/Views/Employee/DepartmentList.cshtml");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDepartmentList()
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT 
                            d.rowAutoID,
                            d.code,
                            d.deptID,
                            d.name,
                            d.director,
                            (SELECT TOP 1 name FROM Person 
                             WHERE SUBSTRING(personID, 1, CHARINDEX('-', personID + '-') - 1) = d.director 
                             AND RIGHT(personID, 2) = '-1') as directorName,
                            d.phone,
                            d.email,
                            d.Emergency_Contacts as EmergencyContacts,
                            d.Emergency_TEL as EmergencyTel,
                            d.modifyTime as LastModified,
                            d.operator as LastModifiedBy,
                            COUNT(DISTINCT SUBSTRING(p2.personID, 1, CHARINDEX('-', p2.personID + '-') - 1)) as EmployeeCount,
                            SUM(CASE 
                                WHEN p2.useCategory = 'Normal card' 
                                    AND RIGHT(p2.personID, 2) = '-1' THEN 1 
                                ELSE 0 
                            END) as ActiveEmployees,
                            SUM(CASE 
                                WHEN (p2.useCategory != 'Normal card' OR p2.useCategory IS NULL)
                                    AND RIGHT(p2.personID, 2) = '-1' THEN 1 
                                ELSE 0 
                            END) as InactiveEmployees
                        FROM Dept d
                        LEFT JOIN Person p2 ON d.deptID = p2.deptID
                        GROUP BY 
                            d.rowAutoID, d.code, d.deptID, d.name, 
                            d.director, d.phone, d.email,
                            d.Emergency_Contacts, d.Emergency_TEL,
                            d.modifyTime, d.operator
                        ORDER BY d.code";

                    var result = await connection.QueryAsync(sql);

                    // คำนวณสรุป
                    var summary = new
                    {
                        total = result.Count(),
                        activeCount = result.Count(r => !string.IsNullOrEmpty(r.director)),
                        withEmergencyContact = result.Count(r =>
                            !string.IsNullOrEmpty(r.EmergencyContacts)
                        ),
                        totalEmployees = result.Sum(r => (int)r.EmployeeCount),
                        activeEmployees = result.Sum(r => (int)r.ActiveEmployees),
                        inactiveEmployees = result.Sum(r => (int)r.InactiveEmployees),
                    };

                    return Json(
                        new
                        {
                            success = true,
                            data = result,
                            summary = summary,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentById(string code)
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT 
                            code,
                            name 
                        FROM Dept 
                        ORDER BY name";

                    var result = await connection.QueryAsync(sql);
                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesByDeptId(string deptId)
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT 
                            personID,
                            name,
                            useCategory,
                            jobPositionID,
                            phone,
                            email,
                            cardNumber,
                            englishName,
                            deptName,
                            CONVERT(varchar, inaugurationDate, 103) as startDate
                        FROM Person 
                        WHERE deptID = @deptId
                        AND RIGHT(personID, 2) = '-1'
                        ORDER BY useCategory DESC, name";

                    var employees = await connection.QueryAsync(sql, new { deptId });
                    return Json(new { success = true, data = employees });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeList()
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT DISTINCT
                            SUBSTRING(personID, 1, CHARINDEX('-', personID + '-') - 1) as empNo,
                            MIN(name) as name,
                            MIN(jobPositionID) as position
                        FROM Person 
                        WHERE useCategory = 'Normal card'
                        GROUP BY SUBSTRING(personID, 1, CHARINDEX('-', personID + '-') - 1)
                        ORDER BY name";

                    var employees = await connection.QueryAsync(sql);
                    return Json(new { success = true, data = employees });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDepartment([FromBody] Dept dept)
        {
            try
            {
                // ตรวจสอบข้อมูลที่จำเป็น
                if (string.IsNullOrEmpty(dept.code))
                {
                    return Json(new { success = false, message = "กรุณาระบุรหัสแผนก" });
                }

                if (string.IsNullOrEmpty(dept.name))
                {
                    return Json(new { success = false, message = "กรุณาระบุชื่อแผนก" });
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่ามีแผนกนี้อยู่จริงหรือไม่
                    var checkSql = "SELECT COUNT(*) FROM Dept WHERE code = @code";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkSql,
                        new { dept.code }
                    );

                    if (exists == 0)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลแผนกที่ต้องการแก้ไข" }
                        );
                    }

                    var sql =
                        @"
                        UPDATE Dept 
                        SET 
                            name = @name,
                            director = @directorId,
                            phone = @phone,
                            email = @email,
                            Emergency_Contacts = @EmergencyContacts,
                            Emergency_TEL = @EmergencyTel,
                            note = @note,
                            modifyTime = GETDATE(),
                            operator = @modifiedBy
                        WHERE code = @code";

                    await connection.ExecuteAsync(
                        sql,
                        new
                        {
                            dept.code,
                            dept.name,
                            directorId = dept.director,
                            dept.phone,
                            dept.email,
                            EmergencyContacts = dept.Emergency_Contacts,
                            EmergencyTel = dept.Emergency_TEL,
                            dept.note,
                            modifiedBy = HttpContext.Session.GetString("Username"),
                        }
                    );

                    return Json(new { success = true, message = "บันทึกข้อมูลเรียบร้อย" });
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error updating department: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(string code)
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่ามีพนักงานในแผนกหรือไม่
                    var checkSql =
                        @"
                        SELECT COUNT(*) FROM Person 
                        WHERE deptID = (SELECT deptID FROM Dept WHERE code = @code)
                        AND RIGHT(personID, 2) = '-1'";

                    var employeeCount = await connection.ExecuteScalarAsync<int>(
                        checkSql,
                        new { code }
                    );

                    if (employeeCount > 0)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่สามารถลบแผนกได้เนื่องจากยังมีพนักงานในแผนก",
                            }
                        );
                    }

                    var sql = "DELETE FROM Dept WHERE code = @code";
                    await connection.ExecuteAsync(sql, new { code });

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployeeDepartment(string personId, string newDeptId)
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่ามีแผนกที่ต้องการย้ายไปอยู่จริงหรือไม่
                    var checkDeptSql = "SELECT name FROM Dept WHERE deptID = @deptId";
                    var deptName = await connection.QueryFirstOrDefaultAsync<string>(
                        checkDeptSql,
                        new { deptId = newDeptId }
                    );

                    if (string.IsNullOrEmpty(deptName))
                    {
                        return Json(new { success = false, message = "ไม่พบแผนกที่ต้องการย้ายไป" });
                    }

                    // อัพเดทแผนกของพนักงาน
                    var basePersonId = personId.Substring(0, personId.Length - 2);
                    var sql =
                        @"
                        UPDATE Person 
                        SET 
                            deptID = @newDeptId,
                            deptName = @deptName,
                            modifyTime = GETDATE(),
                            operator = @modifiedBy
                        WHERE personID LIKE @basePersonId + '%'";

                    await connection.ExecuteAsync(
                        sql,
                        new
                        {
                            newDeptId,
                            deptName,
                            basePersonId,
                            modifiedBy = HttpContext.Session.GetString("Username"),
                        }
                    );

                    return Json(new { success = true, message = "ย้ายแผนกเรียบร้อย" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT 
                            deptID,
                            name
                        FROM Dept
                        ORDER BY name";

                    var departments = await connection.QueryAsync(sql);
                    return Json(new { success = true, data = departments });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
