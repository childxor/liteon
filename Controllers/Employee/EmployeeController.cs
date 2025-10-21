using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Extensions; // เพิ่ม namespace สำหรับ Extension Methods
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Employee
{
    public class EmployeeController : BaseController
    {
        private class SQL944DataResult
        {
            public HashSet<string> sql944Employees { get; set; }
            public Dictionary<string, object> cardDict { get; set; } =
                new Dictionary<string, object>();
            public Dictionary<string, object> fingerprintDict { get; set; } =
                new Dictionary<string, object>();
            public Dictionary<string, string> nameDict { get; set; }
            public Dictionary<string, string> deptNameDict { get; set; }
            public Dictionary<string, string> deptIDDict { get; set; }
            public Dictionary<string, string> cardNumberDict { get; set; }
            public Dictionary<string, int> accessCountDict { get; set; }
            public HashSet<string> fingerprintDataIds { get; set; }
        }

        private readonly string _connectionString;

        private readonly string _hrIpsConnectionString;

        private readonly IConfiguration _configuration;

        private readonly string _oracleConnectionString;

        // sql944
        private readonly string _sql944ConnectionString;

        // เพิ่มตัวแปรสำหรับเชื่อมต่อกับ CES941
        private readonly string _ces941ConnectionString;

        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<EmployeeController> logger
        )
            : base(context)
        {
            _connectionString = _context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = _configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = _configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = _configuration.GetConnectionString("CES941");
            _oracleConnectionString = _configuration.GetConnectionString("OracleConnection");

            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Employee()
        {
            try
            {
                // ดึงข้อมูลแผนก
                using (var defaultconnection = new SqlConnection(_connectionString))
                using (var connection = new SqlConnection(_sql944ConnectionString))
                using (var connectionces = new SqlConnection(_ces941ConnectionString))
                {
                    // เพิ่มข้อมูล Shiftdept
                    var shiftDeptQuery =
                        @"SELECT DISTINCT WrkPlanID as wrkPlanID
                        FROM vEmployee
                        WHERE (WrkPlanID IS NOT NULL)
                        ORDER BY WrkPlanID";
                    var shiftDept = await connectionces.QueryAsync<dynamic>(shiftDeptQuery);
                    ViewBag.Shiftdept = shiftDept;

                    var deptQuery = @"SELECT DISTINCT WorkArea as deptID
                        FROM vEmployee
                        WHERE (WorkArea IS NOT NULL)
                        ORDER BY WorkArea";
                    var dept = await connectionces.QueryAsync<dynamic>(deptQuery);
                    ViewBag.Dept = dept;

                    // เพิ่มการดึงข้อมูลประตู
                    var doorsSql =
                        @"SELECT DISTINCT doorID, doorName 
                        FROM PubDoor 
                        ORDER BY doorName";
                    var doors = await connection.QueryAsync<dynamic>(doorsSql);
                    ViewBag.Doors = doors;

                    // shift dept
                    var shiftfromces941Query =
                        @"SELECT WrkPlanID
                        FROM MEmpGroup
                        ORDER BY WrkPlanID";
                    var shiftfromces941 = await connectionces.QueryAsync<dynamic>(shiftfromces941Query);
                    ViewBag.Shiftfromces941 = shiftfromces941;
                }

                await LoadPermissions("Employee", "Employee");

                // ส่งข้อมูลแผนกของผู้ใช้ไปยัง View
                ViewData["CurrentUserDepartment"] = HttpContext.Session.GetString("WorkArea");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Employee action: {ex.Message}");
                return View();
            }
        }

        public IActionResult GetDataFromSQL944()
        {
            using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
            {
                string sql = "SELECT * FROM Person";
                var data = db.Query<dynamic>(sql);
                return Json(data);
            }
        }

        // แยกส่วนการตรวจสอบพนักงานซ้ำ
        private async Task<bool> IsEmployeeExists(string cardNumber)
        {
            var sql = "SELECT TOP 1 1 FROM emp_person WHERE cardNumber = @cardNumber";
            using (var connection = new SqlConnection(_connectionString))
            {
                var exists = await connection.QueryFirstOrDefaultAsync<int?>(
                    sql,
                    new { cardNumber = cardNumber }
                );
                return exists.HasValue;
            }
        }

        // แยกส่วนการสร้างข้อมูลพื้นฐานของพนักงาน
        private emp_person CreateBaseEmployee(emp_person employee)
        {
            return new emp_person
            {
                cardNumber = employee.cardNumber,
                name = employee.name,
                deptID = employee.deptID,
                deptName = employee.deptName,
                CREATEDATE = DateTime.Now,
                RecordStatus = "N",
                status = "0",
                isActive = 1,
                leaveJobDate = new DateTime(1999, 12, 31),
                enableDate = new DateTime(2024, 7, 24),
                disableDate = new DateTime(1999, 12, 31),
                userLevel = "29991231",
                modifyTime = new DateTime(2024, 8, 7, 9, 2, 5),
                cardCategory = "0",
                cardStatus = "129",
                cardType = "0",
                cardTypeDesc = "Normal card",
                reserveChar1 = "19991231",
                subSystem = "11111111111111111111",
                useCategory = "Normal card",
                useStatus = "2",
            };
        }

        // แยกส่วนการบันทึกข้อมูลลง SQL944
        private async Task SaveToSQL944(emp_person employee1, emp_person employee2)
        {
            using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
            {
                var sql944Insert =
                    @"
                    INSERT INTO Person (
                        CardNumber, PersonID, Name, DeptID, DeptName,
                        UserLevel, Status, ReserveChar1, CardType, CardTypeDesc,
                        SubSystem, UseCategory, UseStatus, CardCategory, CardStatus,
                        reserve3, modifyTime, leaveJobDate, enableDate, disableDate
                    )
                    VALUES (
                        @CardNumber, @PersonID, @Name, @DeptID, @DeptName,
                        @UserLevel, @Status, @ReserveChar1, @CardType, @CardTypeDesc,
                        @SubSystem, @UseCategory, @UseStatus, @CardCategory, @CardStatus,
                        @reserve3, @modifyTime, @leaveJobDate, @enableDate, @disableDate
                    )";

                // บันทึกข้อมูลพนักงานคนที่ 1
                await sql944Connection.ExecuteAsync(sql944Insert, MapEmployeeToSQL944(employee1));
                // บันทึกข้อมูลพนักงานคนที่ 2
                await sql944Connection.ExecuteAsync(sql944Insert, MapEmployeeToSQL944(employee2));
            }
        }

        // แยกส่วน mapping ข้อมูลสำหรับ SQL944
        private object MapEmployeeToSQL944(emp_person employee)
        {
            return new
            {
                CardNumber = employee.cardNumber,
                PersonID = employee.personID,
                Name = employee.name,
                DeptID = employee.deptID,
                DeptName = employee.deptName,
                UserLevel = employee.userLevel,
                Status = employee.status,
                ReserveChar1 = employee.reserveChar1,
                CardType = employee.cardType,
                CardTypeDesc = employee.cardTypeDesc,
                SubSystem = employee.subSystem,
                UseCategory = employee.useCategory,
                UseStatus = employee.useStatus,
                CardCategory = employee.cardCategory,
                CardStatus = employee.cardStatus,
                reserve3 = employee.reserve3,
                modifyTime = DateTime.Now,
                leaveJobDate = new DateTime(1999, 12, 31),
                enableDate = DateTime.Now,
                disableDate = new DateTime(1999, 12, 31),
            };
        }

        // ยืนยันการลบ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(string personId)
        {
            try
            {
                var basePersonId = personId.Split('-')[0];

                // ตรวจสอบสถานะปัจจุบันของข้อมูล
                bool isAlreadyMarkedForDeletion = false;
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var noteQuery = "SELECT note FROM Person WHERE personID = @personId";
                    var noteResult = await connection.QueryFirstOrDefaultAsync<string>(
                        noteQuery,
                        new { personId = basePersonId + "-1" }
                    );

                    isAlreadyMarkedForDeletion = !string.IsNullOrEmpty(noteResult) &&
                                               noteResult.Trim().Equals("del", StringComparison.OrdinalIgnoreCase);
                }

                // ดึงข้อมูลแผนกของพนักงานที่ต้องการลบ
                string employeeDeptId = "";
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var deptQuery =
                        "SELECT DeptID FROM Person WHERE personID = @personId OR personID = @personId1 OR personID = @personId2";
                    var deptResult = await connection.QueryFirstOrDefaultAsync<string>(
                        deptQuery,
                        new
                        {
                            personId = personId,
                            personId1 = personId + "-1",
                            personId2 = personId + "-2",
                        }
                    );
                    employeeDeptId = deptResult ?? "";
                }

                // ดึงข้อมูลพนักงานจากฐานข้อมูลเพื่อตรวจสอบแผนก
                var employee = await _context.emp_person.FirstOrDefaultAsync(e =>
                    e.personID.StartsWith(basePersonId)
                );
                if (employee != null)
                {
                    // ตรวจสอบสิทธิ์การเข้าถึงข้อมูลพนักงาน
                    if (!CanAccessEmployeeData(employee.deptID))
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่มีสิทธิ์ในการลบข้อมูลพนักงานในแผนกนี้",
                            }
                        );
                    }
                }

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                {
                    if (isAlreadyMarkedForDeletion)
                    {
                        // ครั้งที่ 2: ลบจริงๆ
                        await sql944Connection.ExecuteAsync(
                            @"DELETE FROM Person WHERE PersonID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        await sql944Connection.ExecuteAsync(
                            @"DELETE FROM Person_FP WHERE CardNumber = @basePersonId",
                            new { basePersonId = basePersonId }
                        );

                        await sql944Connection.ExecuteAsync(
                            @"DELETE FROM PubDoorAuth WHERE PersonID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        await hrConnection.ExecuteAsync(
                            @"DELETE FROM emp_person WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        await _context.Database.ExecuteSqlRawAsync(
                            @"DELETE FROM emp_person WHERE personID IN ({0}, {1})",
                            basePersonId + "-1",
                            basePersonId + "-2"
                        );

                        return Json(new { success = true, message = "ลบข้อมูลถาวรเรียบร้อยแล้ว" });
                    }
                    else
                    {
                        // ครั้งแรก: ทำเครื่องหมายเพื่อลบ (update note = 'del')
                        await sql944Connection.ExecuteAsync(
                            @"UPDATE Person SET note = 'del' WHERE PersonID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // ปิดสิทธิ์ประตู
                        await sql944Connection.ExecuteAsync(
                            @"UPDATE PubDoorAuth SET reserve1 = 2 WHERE PersonID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        return Json(new { success = true, message = "ทำเครื่องหมายเพื่อลบเรียบร้อยแล้ว คลิกอีกครั้งเพื่อลบถาวร" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Rollback ข้อมูลที่ถูกทำเครื่องหมายเพื่อลบ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RollbackEmployee(string personId)
        {
            try
            {
                var basePersonId = personId.Split('-')[0];

                // ตรวจสอบสิทธิ์การเข้าถึงข้อมูลพนักงาน
                var employee = await _context.emp_person.FirstOrDefaultAsync(e =>
                    e.personID.StartsWith(basePersonId)
                );
                if (employee != null)
                {
                    if (!CanAccessEmployeeData(employee.deptID))
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่มีสิทธิ์ในการจัดการข้อมูลพนักงานในแผนกนี้",
                            }
                        );
                    }
                }

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                {
                    // Rollback note = null ใน Person table ของ SQL944
                    await sql944Connection.ExecuteAsync(
                        @"UPDATE Person SET note = NULL WHERE PersonID IN (@personId1, @personId2)",
                        new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                    );

                    // เปิดสิทธิ์ประตูพื้นฐานใหม่
                    await sql944Connection.ExecuteAsync(
                        @"UPDATE PubDoorAuth SET reserve1 = NULL WHERE PersonID IN (@personId1, @personId2)",
                        new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                    );

                    // เพิ่มสิทธิ์ประตูพื้นฐานถ้ายังไม่มี
                    await AddBasicDoorAccessForEmployee(basePersonId, sql944Connection);

                    return Json(new { success = true, message = "คืนสถานะข้อมูลเรียบร้อยแล้ว" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Helper method สำหรับเพิ่มสิทธิ์ประตูพื้นฐาน (อิงจาก AddHRAccess)
        private async Task AddBasicDoorAccessForEmployee(string personId, SqlConnection connection)
        {
            try
            {
                // ลบสิทธิ์เก่าที่อาจมี reserve1 = 2
                await connection.ExecuteAsync(
                    @"DELETE FROM PubDoorAuth WHERE PersonID IN (@personId1, @personId2) AND (TRY_CAST(reserve1 AS INT) = 2 OR reserve1 = '2')",
                    new { personId1 = personId + "-1", personId2 = personId + "-2" }
                );

                // ดึงข้อมูลประตูพื้นฐานที่พนักงานทุกคนควรมี (เหมือน AddHRAccess)
                var basicDoors = await connection.QueryAsync<dynamic>(
                    @"SELECT doorID, weekTimeID 
                      FROM PubDoor 
                      LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1
                      WHERE doorName LIKE '%Build1 - HR Attendance%' 
                      OR doorName LIKE '%Comsumables room%' 
                      AND doorID NOT IN ('0000301', '0000401', '0000501', '0000601', '0000701', '0000801', '0000901','0001501')"
                );

                foreach (var door in basicDoors)
                {
                    // เพิ่มสิทธิ์สำหรับบัตร (-1)
                    await AddDoorAccess(
                        personId + "-1",
                        door.doorID,
                        door.weekTimeID ?? "1", // ใช้ default weekTimeID ถ้าไม่มี
                        0, // DNFP = 0 สำหรับบัตร
                        connection
                    );

                    // ตรวจสอบว่ามีลายนิ้วมืออยู่หรือไม่
                    var hasFingerprintData = await connection.QueryFirstOrDefaultAsync<int>(
                        @"SELECT COUNT(1) FROM Person WHERE personID = @personId",
                        new { personId = personId + "-2" }
                    );

                    // ถ้ามีลายนิ้วมือ ให้เพิ่มสิทธิ์สำหรับลายนิ้วมือ (-2)
                    if (hasFingerprintData > 0)
                    {
                        await AddDoorAccess(
                            personId + "-2",
                            door.doorID,
                            door.weekTimeID ?? "1", // ใช้ default weekTimeID ถ้าไม่มี
                            1, // DNFP = 1 สำหรับลายนิ้วมือ
                            connection
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent rollback failure
                _logger.LogError($"Error adding basic door access for {personId}: {ex.Message}");
            }
        }

        // ฟังกชั่นหลัก RemoveEmployee ออกจาก sql944
        [HttpPost]
        public async Task<IActionResult> RemoveEmployee(string personId)
        {
            try
            {
                // ลบข้อมูลจากฐานข้อมูล sql944
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    await sql944Connection.ExecuteAsync(
                        "DELETE FROM Person WHERE PersonID = @personID",
                        new { personID = personId }
                    );
                }

                return Json(new { success = true, message = "ลบข้อมูลเรียบร้อยแล้ว" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ฟังก์ชันใหม่สำหรับลบพนักงานที่ลาออกทั้งหมดออกจาก SQL944
        [HttpPost]
        public async Task<IActionResult> RemoveAllResignedEmployees()
        {
            try
            {
                int deletedCount = 0;

                // ดึงรายการพนักงานที่ลาออกแล้วจากฐานข้อมูล ces941
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    var resignedEmployees = await connection.QueryAsync<dynamic>(
                        @"SELECT EmpNo
                        FROM MEmpBasic
                        WHERE EmpResignDate IS NOT NULL"
                    );

                    if (resignedEmployees.Any())
                    {
                        // ลบข้อมูลพนักงานที่ลาออกแล้วจาก SQL944
                        using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                        {
                            foreach (var employee in resignedEmployees)
                            {
                                string personId = employee.EmpNo.ToString();
                                string personId1 = personId + "-1";
                                string personId2 = personId + "-2";
                                try
                                {
                                    // ลบข้อมูลจาก Person ใน SQL944
                                    await sql944Connection.ExecuteAsync(
                                        "DELETE FROM Person WHERE PersonID IN (@personID1, @personID2)",
                                        new { personID1 = personId1, personID2 = personId2 }
                                    );

                                    deletedCount++;
                                }
                                catch (Exception ex)
                                {
                                    // บันทึก log ข้อผิดพลาดแต่ดำเนินการต่อ
                                    Console.WriteLine(
                                        $"เกิดข้อผิดพลาดในการลบพนักงาน {personId}: {ex.Message}"
                                    );
                                }
                            }
                        }
                    }
                }

                return Json(
                    new
                    {
                        success = true,
                        message = $"ลบข้อมูลพนักงานที่ลาออกแล้วจำนวน {deletedCount} คนเรียบร้อยแล้ว",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ฟังกชั่นหลัก SaveEmployee ที่ปรับปรุงใหม่
        [HttpPost]
        public async Task<IActionResult> SaveEmployee([FromBody] emp_person employee)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ตรวจสอบว่า personID มี -1 หรือ -2 ต่อท้ายหรือไม่
                    string basePersonId = employee.personID;
                    bool isCardData = false;
                    bool isFingerprintData = false;

                    if (employee.personID.EndsWith("-1"))
                    {
                        basePersonId = employee.personID.Substring(0, employee.personID.Length - 2);
                        isCardData = true;
                    }
                    else if (employee.personID.EndsWith("-2"))
                    {
                        basePersonId = employee.personID.Substring(0, employee.personID.Length - 2);
                        isFingerprintData = true;
                    }

                    // ค้นหาพนักงานจาก personID (ไม่มี -1 หรือ -2)
                    var existingEmployees = await _context
                        .emp_person.Where(e => e.personID.StartsWith(basePersonId))
                        .ToListAsync();

                    if (existingEmployees.Any())
                    {
                        // อัพเดทข้อมูลในฐานข้อมูลหลัก
                        foreach (var existingEmployee in existingEmployees)
                        {
                            existingEmployee.name = employee.name;
                            existingEmployee.deptID = employee.deptID;
                            existingEmployee.deptName = employee.deptName;
                            existingEmployee.shiftMent = employee.shiftMent;
                            existingEmployee.modifyTime = DateTime.Now;

                            // กำหนด cardNumber ตามเงื่อนไข
                            if (existingEmployee.personID.EndsWith("-1"))
                            {
                                // สำหรับบัตร ใช้ cardNumber ที่ส่งมา
                                existingEmployee.cardNumber = employee.cardNumber;
                            }
                            else if (existingEmployee.personID.EndsWith("-2"))
                            {
                                // สำหรับลายนิ้วมือ ใช้ basePersonId เป็น cardNumber
                                existingEmployee.cardNumber = basePersonId;
                            }
                        }

                        await _context.SaveChangesAsync();

                        // อัพเดทข้อมูลใน HR_IPS
                        var updateHrSql =
                            @"
                            UPDATE emp_person 
                            SET name = @name,
                                deptID = @deptID,
                                deptName = @deptName,
                                shiftMent = @shiftMent,
                                cardNumber = @cardNumber,
                                modifyTime = @modifyTime
                            WHERE personID = @personID";

                        // อัพเดทข้อมูลใน SQL944
                        var updateSql944 =
                            @"
                            UPDATE Person 
                            SET Name = @name,
                                DeptID = @deptID,
                                DeptName = @deptName,
                                CardNumber = @cardNumber,
                                modifyTime = @modifyTime
                            WHERE PersonID = @personID";

                        foreach (var existingEmployee in existingEmployees)
                        {
                            string cardNumberToUse;
                            if (existingEmployee.personID.EndsWith("-1"))
                            {
                                // สำหรับบัตร ใช้ cardNumber ที่ส่งมา
                                cardNumberToUse = employee.cardNumber;
                            }
                            else
                            {
                                // สำหรับลายนิ้วมือ ใช้ basePersonId เป็น cardNumber
                                cardNumberToUse = basePersonId;
                            }

                            var parameters = new
                            {
                                name = employee.name,
                                deptID = employee.deptID,
                                deptName = employee.deptName,
                                cardNumber = cardNumberToUse,
                                modifyTime = DateTime.Now,
                                personID = existingEmployee.personID,
                                shiftMent = employee.shiftMent,
                            };

                            // อัพเดทข้อมูลในตาราง emp_person_shift
                            var updateEmpPersonShiftSql =
                                @"
                                UPDATE emp_person_shift 
                                SET name = @name,
                                    deptCode = @deptID,
                                    deptName = @deptName,
                                    deptID = @deptID,
                                    date = @modifyTime,
                                    isActive = 1
                                WHERE personID = @personID";

                            var sqlParameters = new[]
                            {
                                new SqlParameter("@name", parameters.name),
                                new SqlParameter("@deptID", parameters.deptID),
                                new SqlParameter("@deptName", parameters.deptName),
                                new SqlParameter("@modifyTime", parameters.modifyTime),
                                new SqlParameter("@personID", basePersonId),
                            };

                            await _context.Database.ExecuteSqlRawAsync(
                                updateEmpPersonShiftSql,
                                sqlParameters
                            );
                            await hrConnection.ExecuteAsync(updateHrSql, parameters);
                            await sql944Connection.ExecuteAsync(updateSql944, parameters);
                        }

                        return Json(new { success = true, message = "อัพเดทข้อมูลสำเร็จ" });
                    }
                    else
                    {
                        // ถ้าส่งมาเป็น personId ที่มี -1 หรือ -2 ต่อท้าย ให้ใช้ basePersonId เป็นฐาน
                        string personId1 = basePersonId + "-1";
                        string personId2 = basePersonId + "-2";

                        // เพิ่มข้อมูลใหม่ในฐานข้อมูลหลัก
                        var employee1 = new emp_person
                        {
                            personID = personId1,
                            cardNumber = employee.cardNumber, // บัตรใช้ cardNumber ที่ส่งมา
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            shiftMent = employee.shiftMent,
                            CREATEDATE = DateTime.Now,
                            isActive = 1,
                            userLevel = "29991231",
                            status = "1",
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                        };

                        var employee2 = new emp_person
                        {
                            personID = personId2,
                            cardNumber = basePersonId, // ลายนิ้วมือใช้ basePersonId เป็น cardNumber
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            shiftMent = employee.shiftMent,
                            CREATEDATE = DateTime.Now,
                            isActive = 1,
                            userLevel = "29991231",
                            status = "1",
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                        };

                        await _context.emp_person.AddRangeAsync(employee1, employee2);
                        await _context.SaveChangesAsync();

                        // เพิ่มข้อมูลใน HR_IPS
                        var insertHrSql =
                            @"
                            INSERT INTO emp_person (
                                personID, cardNumber, name, deptID, deptName, 
                                shiftMent, isActive, userLevel, 
                                status, createDate,leaveJobDate,enableDate,disableDate,
                                cardType,cardTypeDesc,subSystem,useCategory,useStatus
                            ) VALUES (
                                @personID, @cardNumber, @name, @deptID, @deptName,
                                @shiftMent, @isActive, @userLevel,
                                @status, @createDate,@leaveJobDate,@enableDate,@disableDate,
                                @cardType,@cardTypeDesc,@subSystem,@useCategory,@useStatus
                            )";

                        // เพิ่มข้อมูลใน SQL944
                        var insertSql944 =
                            @"
                            INSERT INTO Person (
                                PersonID, CardNumber, Name, DeptID, DeptName,
                                UserLevel, Status, CardType, CardTypeDesc,
                                SubSystem, UseCategory, UseStatus, CardCategory,
                                CardStatus, modifyTime, leaveJobDate, enableDate,
                                disableDate,reserve3,reserveChar1
                            ) VALUES (
                                @personID, @cardNumber, @name, @deptID, @deptName,
                                @userLevel, @status, '0', 'Normal card',
                                '11111111111111111111', 'Normal card', '2', '0',
                                '129', @modifyTime, @leaveJobDate, @enableDate, @disableDate,
                                @reserve3,@reserveChar1
                            )";

                        // สร้าง parameters สำหรับ SQL944 - บัตร
                        var sql944Parameters1 = new
                        {
                            personID = personId1,
                            cardNumber = employee.cardNumber,
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            userLevel = "29991231",
                            status = "1",
                            modifyTime = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            reserve3 = employee.cardNumber,
                            reserveChar1 = "19991231",
                        };

                        // สร้าง parameters สำหรับ HR_IPS - บัตร
                        var hrParameters1 = new
                        {
                            personID = personId1,
                            cardNumber = employee.cardNumber,
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            shiftMent = employee.shiftMent,
                            isActive = 1,
                            userLevel = "29991231",
                            status = "1",
                            createDate = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            cardType = "0",
                            cardTypeDesc = "Normal card",
                            subSystem = "11111111111111111111",
                            useCategory = "Normal card",
                            useStatus = "2",
                            RecordStatus = 1,
                            reserve3 = employee.cardNumber,
                            reserveChar1 = "19991231",
                        };

                        // ทำการ insert ข้อมูลบัตร
                        await hrConnection.ExecuteAsync(insertHrSql, hrParameters1);
                        await sql944Connection.ExecuteAsync(insertSql944, sql944Parameters1);

                        // สร้าง parameters สำหรับ SQL944 - ลายนิ้วมือ
                        var sql944Parameters2 = new
                        {
                            personID = personId2,
                            cardNumber = basePersonId,
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            userLevel = "29991231",
                            status = "1",
                            modifyTime = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            reserve3 = basePersonId,
                            reserveChar1 = "19991231",
                        };

                        // สร้าง parameters สำหรับ HR_IPS - ลายนิ้วมือ
                        var hrParameters2 = new
                        {
                            personID = personId2,
                            cardNumber = basePersonId,
                            name = employee.name,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            shiftMent = employee.shiftMent,
                            isActive = 1,
                            userLevel = "29991231",
                            status = "1",
                            createDate = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            cardType = "0",
                            cardTypeDesc = "Normal card",
                            subSystem = "11111111111111111111",
                            useCategory = "Normal card",
                            useStatus = "2",
                            RecordStatus = 1,
                            reserve3 = basePersonId,
                            reserveChar1 = "19991231",
                        };

                        // ทำการ insert ข้อมูลลายนิ้วมือ
                        await hrConnection.ExecuteAsync(insertHrSql, hrParameters2);
                        await sql944Connection.ExecuteAsync(insertSql944, sql944Parameters2);

                        return Json(new { success = true, message = "เพิ่มข้อมูลพนักงานสำเร็จ" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        private async Task SaveToHR_IPS(emp_person employee)
        {
            // บันทึกข้อมูลใน HR_IPS
            using (var hrIpsConnection = new SqlConnection(_hrIpsConnectionString))
            {
                var insertSql =
                    "INSERT INTO emp_person (cardNumber, personID, name, deptID, deptName, shiftMent, isActive, RecordStatus, userLevel, status, CREATEDATE, cardType, cardTypeDesc, subSystem, useCategory, useStatus, reserve3, reserveChar1) "
                    + "VALUES (@cardNumber, @personID, @name, @deptID, @deptName, @shiftMent, @isActive, @RecordStatus, @userLevel, @status, GETDATE(), @cardType, @cardTypeDesc, @subSystem, @useCategory, @useStatus, @reserve3, @reserveChar1)";
                await hrIpsConnection.ExecuteAsync(insertSql, employee);
            }
        }

        private async Task SaveToSQL944(emp_person employee)
        {
            // บันทึกข้อมูลใน SQL944
            using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
            {
                var insertSql =
                    "INSERT INTO Person (cardNumber, personID, name, deptID, deptName, shiftMent, isActive, RecordStatus, userLevel, status, CREATEDATE) VALUES (@cardNumber, @personID, @name, @deptID, @deptName, @shiftMent, @isActive, @RecordStatus, @userLevel, @status, getdate())";
                await sql944Connection.ExecuteAsync(insertSql, employee);
            }
        }

        // แยกส่วนการอัพเดทข้อมูลที่มีอยู่
        private async Task UpdateExistingEmployee(emp_person employee)
        {
            var existingEmployees = await _context
                .emp_person.Where(e => e.cardNumber == employee.cardNumber)
                .ToListAsync();

            if (existingEmployees.Any())
            {
                foreach (var existingEmployee in existingEmployees)
                {
                    existingEmployee.name = employee.name;
                    existingEmployee.deptID = employee.deptID;
                    existingEmployee.deptName = employee.deptName;
                    existingEmployee.modifyTime = DateTime.Now;
                    existingEmployee.CREATEDATE = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // เพิ่มใน HR_IPS และ SQL944 ด้วย
                await SaveToHR_IPS(employee);
                await SaveToSQL944(employee);
            }

            // อัพเดทข้อมูลใน SQL944
            using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
            using (var hrIpsConnection = new SqlConnection(_hrIpsConnectionString))
            {
                var updateSql =
                    @"
                    UPDATE Person 
                    SET Name = @Name,
                        DeptID = @DeptID,
                        DeptName = @DeptName,
                        cardNumber = @cardNumber,
                        modifyTime = @ModifyTime
                    WHERE PersonID = @PersonIDPattern";

                var parameters1 = new
                {
                    Name = employee.name,
                    DeptID = employee.deptID,
                    DeptName = employee.deptName,
                    ModifyTime = DateTime.Now,
                    cardNumber = employee.personID,
                    PersonIDPattern = employee.personID + "-1",
                    CREATEDATE = DateTime.Now,
                    cardType = "0",
                    cardTypeDesc = "Normal card",
                    subSystem = "11111111111111111111",
                    useCategory = "Normal card",
                    useStatus = "2",
                    reserve3 = employee.personID, // เพิ่ม reserve3
                    reserveChar1 = "19991231" // เพิ่ม reserveChar1
                    ,
                };

                var parameters2 = new
                {
                    Name = employee.name,
                    DeptID = employee.deptID,
                    DeptName = employee.deptName,
                    ModifyTime = DateTime.Now,
                    cardNumber = employee.cardNumber,
                    PersonIDPattern = employee.personID + "-2",
                    CREATEDATE = DateTime.Now,
                    cardType = "0",
                    cardTypeDesc = "Normal card",
                    subSystem = "11111111111111111111",
                    useCategory = "Normal card",
                    useStatus = "2",
                    reserve3 = employee.cardNumber, // เพิ่ม reserve3
                    reserveChar1 = "19991231" // เพิ่ม reserveChar1
                    ,
                };

                await sql944Connection.ExecuteAsync(updateSql, parameters1);
                await sql944Connection.ExecuteAsync(updateSql, parameters2);
                await hrIpsConnection.ExecuteAsync(updateSql, parameters1);
                await hrIpsConnection.ExecuteAsync(updateSql, parameters2);
                await _context.SaveChangesAsync();
            }
        }

        // เพิ่ม Method สำหรับอัพเดทข้อมูลพนักงาน
        [HttpPost]
        [Route("Employee/UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee(
            string personID,
            string cardNumber,
            string name,
            string deptID,
            string deptName
        )
        {
            try
            {
                // ตรวจสอบว่ามีการระบุข้อมูลครบถ้วนหรือไม่
                if (
                    string.IsNullOrEmpty(personID)
                    || string.IsNullOrEmpty(cardNumber)
                    || string.IsNullOrEmpty(name)
                )
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                // อัพเดทข้อมูลใน SQL944
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูล PersonID ทั้งบัตรและลายนิ้วมือ
                    string baseId = personID.Split('-')[0];
                    var personIds = new[] { $"{baseId}-1", $"{baseId}-2" };

                    foreach (var id in personIds)
                    {
                        // ตรวจสอบว่ามีข้อมูลในตาราง Person หรือไม่
                        var exists = await sql944Connection.QueryFirstOrDefaultAsync<int?>(
                            "SELECT 1 FROM Person WHERE personID = @personID",
                            new { personID = id }
                        );

                        if (exists.HasValue)
                        {
                            // อัพเดทข้อมูลในตาราง Person
                            await sql944Connection.ExecuteAsync(
                                @"UPDATE Person 
                                SET cardNumber = @cardNumber,
                                    Name = @name,
                                    DeptID = @deptID,
                                    DeptName = @deptName,
                                    modifyTime = @modifyTime
                                WHERE personID = @personID",
                                new
                                {
                                    cardNumber,
                                    name,
                                    deptID,
                                    deptName,
                                    modifyTime = DateTime.Now,
                                    personID = id,
                                }
                            );
                        }
                    }
                }

                return Json(new { success = true, message = "อัพเดทข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateEmployee: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // Method สำหรับดึงข้อมลแผนก (ตัวอย่าง)
        public List<dynamic> GetDepartments()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    string sql =
                        @"
                        SELECT 
                            rowAutoID, code, parentCode, deptID, 
                            name, pinyin, deptLevel, inID, 
                            inName, director, phone, fax, 
                            address, zip, subSystem
                        FROM Dept 
                        WHERE deptID IS NOT NULL 
                        ORDER BY deptID";
                    var data = db.Query(sql).ToList<dynamic>();

                    // กรองแผนกตามสิทธิ์การเข้าถึง
                    if (data != null && data.Any())
                    {
                        // ถ้าเป็น admin ให้แสดงทั้งหมด
                        if (IsCurrentUserAdmin)
                        {
                            return data;
                        }
                        else
                        {
                            // กรองเฉพาะแผนกที่ผู้ใช้มีสิทธิ์เข้าถึง
                            var filteredData = data.Where(dept =>
                                    !string.IsNullOrEmpty(CurrentUserDepartment)
                                    && !string.IsNullOrEmpty(dept.deptID?.ToString())
                                    && CurrentUserDepartment.Equals(
                                        dept.deptID.ToString(),
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                .ToList();
                            return filteredData;
                        }
                    }

                    return data ?? new List<dynamic>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting departments: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // เพิ่มเมธอดใหม่สำหรับดึงข้อมูลพนักงานจาก CES941
        private async Task<List<dynamic>> GetEmployeesFromCES941(string department)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    // ข้อมูลส่งมาแบบนี้ RM,SCM,SCMP
                    // ต้องแปลงเป็น array เพื่อใช้กับ IN (@departments)
                    var departments = department?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

                    string sql =
                        @"
                        SELECT 
                            EmpNo, PrefixName, EmpName, EmpLName, EmpStartDate, DepCode, 
                            ProbationDay, ProbationPass, ProbationDate, TitleCode, TitleLong, 
                            SecCode, SecDsc, SecShortDsc, EmpPicture, FlagDel, ComNo, BossId, 
                            WorkArea, EmpResignDate, AcceptRehired, LastDateAtten, Language, 
                            Gender, WrkPlanID, CostCenter, EmpType, BirthDate, PersonalID, 
                            DLInd, FuncCode, EmpGroup, JGCode, Critical, JGStruc, FullName, 
                            ShortName, ADUser, PrgnSts, Staff, JGLevel, BankAccount, SubCon, 
                            PreEmpNo, eMailAdd, AdminAction, ResignAlertDate, ResignKeyDate, 
                            PayrollProcess, PrefixCode, MobilePhone, HomePhone, ContZipCode, 
                            ContZipDesc, ContCountry, ContCity, Religion, Ext, SkyUser, 
                            LotusUser, DIHUser, R3User, Internet, JDCode, ADPath, MRP, 
                            Hospital, Nationality, Marital, ServiceDate, ProbationEvident, 
                            InsDown, InsRec, InsEmp, SocialDown, SocialRec, SocialEmp, 
                            OTCondition, calOT, calCPS, AbnormalAdjust, SFID, THOrg, 
                            PassPort, TaxID, LeaveCalendarType, MFG, MROOrg, Expr1, 
                            CBasicDate, CComDate, TitleShort
                        FROM vEmployee
                        WHERE Language = 'en'
                        " + (departments.Length > 0 ? "AND WorkArea IN @departments" : "");

                    var employees = await db.QueryAsync(sql, new { departments });
                    return employees.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching employees from CES941: {ex.Message}");
                return new List<dynamic>();
            }
        }

        private async Task<List<dynamic>> GetShiftsDataHR_IPS()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    string sql =
                        @"
                        SELECT EmpNo, PrefixName, EmpName, EmpLName, EmpStartDate, EmpResignDate, ActionType, ActionReason, PrimaryKey, EffDateFrom, EffDateTo, EffAttProcess, BIWRequire, ActionTypeDesc, TDepCode, TSecCode, 
                         Gender, BirthDate, TCosCenter, TWorkArea, WrkPlanID, TBossId, BrwType, CreateDate, CreateBy, FlagDel, DepCode, SecCode, CostCenter, WorkArea, BossId, ComNo, TComNo, Language, EmpGroup, ReasonCode, ChangeDate,
                          ChangeBy
                        FROM vEmpMovement 
                        WHERE Language = 'EN' 
                        ";
                    var data = await db.QueryAsync(sql);
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching shifts data from HR_IPS: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // ปรับปรุงเมธอด GetEmployees เพื่อดึงข้อมูลจาก CES941 และ SQL944
        [HttpGet]
        [Route("Employee/TestPermissions")]
        public async Task<IActionResult> TestPermissions()
        {
            try
            {
                // ตรวจสอบว่า permissions ถูกโหลดแล้วหรือไม่
                if (CurrentPermissions == null)
                {
                    Console.WriteLine("TestPermissions: Permissions not loaded, loading now...");
                    await LoadPermissions("Employee", "TestPermissions");
                }

                return Json(
                    new
                    {
                        success = true,
                        permissions = CurrentPermissions,
                        userDepartment = CurrentUserDepartment,
                        canView = CheckPermission("CanView"),
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [Route("Employee/GetEmployeesListData")]
        public async Task<IActionResult> GetEmployeesListData(
            string status = "",
            string door = "",
            string department = "", 
            string shift = "",
            string fingerprint = "",
            string pdline = "",
            bool mergeTraining = false
        )
        {
            try
            {
                _logger.LogInformation("GetEmployeesData: Starting to load employee data");

                // ตรวจสอบการเชื่อมต่อฐานข้อมูล
                if (!await TestDatabaseConnections())
                {
                    return Json(new { error = "ไม่สามารถเชื่อมต่อฐานข้อมูลได้ กรุณาตรวจสอบการเชื่อมต่อ" });
                }

                // ตรวจสอบและโหลด permissions
                await EnsurePermissionsLoaded();

                _logger.LogInformation("GetEmployeesData: Permissions loaded, starting data retrieval");

                // ดึงข้อมูลจากแหล่งต่างๆ แบบ parallel เพื่อลดเวลาในการรอ // 并行获取数据以减少等待时间
                var ces941Task = GetEmployeesFromCES941(department);
                var sql944Task = GetSQL944Data(department);
                var shiftsTask = GetShiftsData(); // ดึงขอมูลจาก HRM_IPS
                var jobGradeTask = GetJobGrades(); // เพิ่ม job grade task แบบ parallel

                await Task.WhenAll(ces941Task, sql944Task, shiftsTask, jobGradeTask);

                var ces941Employees = await ces941Task;
                var sql944Data = await sql944Task;
                var shifts = await shiftsTask;
                var jobGradeDict = await jobGradeTask;

                _logger.LogInformation($"GetEmployeesData: Retrieved {ces941Employees?.Count() ?? 0} employees from CES941, {sql944Data?.sql944Employees?.Count ?? 0} from SQL944");

                // สร้าง Dictionary สำหรับข้อมูล CES941 // 创建CES941数据字典
                var ces941Dict = new Dictionary<string, object>();
                foreach (var emp in ces941Employees)
                {
                    if (!string.IsNullOrEmpty(emp.EmpNo?.ToString().Trim()))
                    {
                        ces941Dict[emp.EmpNo.ToString().Trim()] = emp;
                    }
                }

                // ประมวลผลข้อมูล SQL944 // 处理SQL944数据
                var sql944Employees = sql944Data.sql944Employees;
                var cardDict = sql944Data.cardDict;
                var fingerprintDict = sql944Data.fingerprintDict;
                var nameDict = sql944Data.nameDict;
                var deptNameDict = sql944Data.deptNameDict;
                var deptIDDict = sql944Data.deptIDDict;
                var cardNumberDict = sql944Data.cardNumberDict;
                var accessCountDict = sql944Data.accessCountDict;
                var fingerprintDataIds = sql944Data.fingerprintDataIds;

         
                // ดึงข้อมูล training ทั้งหมด (รองรับหลายรายการต่อพนักงาน)
                var trainingStatusDict = await GetTrainingStatusData();
   

                // รวมข้อมูลพนักงาน
                var employeesList = await BuildEmployeesList(
                    ces941Dict,
                    sql944Employees,
                    sql944Data.cardDict,
                    sql944Data.fingerprintDict,
                    sql944Data.nameDict,
                    sql944Data.deptNameDict,
                    sql944Data.deptIDDict,
                    sql944Data.cardNumberDict,
                    sql944Data.accessCountDict,
                    sql944Data.fingerprintDataIds,
                    shifts,
                    jobGradeDict,
                    trainingStatusDict
                );

                _logger.LogInformation($"GetEmployeesData: Built employee list with {employeesList?.Count ?? 0} employees");

                // กรองข้อมูลตามเงื่อนไข
                employeesList = await ApplyFilters(
                    employeesList,
                    status,
                    door,
                    department,
                    shift,
                    fingerprint,
                    pdline
                );

                // Debug logging
                LogEmployeeCount(employeesList.Count);

                return Json(employeesList ?? new List<dynamic>());
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning($"GetEmployeesData: Context disposed error - {ex.Message}");
                return Json(new { error = "เกิดข้อผิดพลาดในการเข้าถึงฐานข้อมูล กรุณาลองใหม่อีกครั้ง" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetEmployees: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { error = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        private async Task<bool> TestDatabaseConnections()
        {
            try
            {
                // ตรวจสอบการเชื่อมต่อฐานข้อมูลหลัก
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection test: Main DB connection successful");
                }

                // ตรวจสอบการเชื่อมต่อ SQL944
                if (!string.IsNullOrEmpty(_sql944ConnectionString))
                {
                    using (var connection = new SqlConnection(_sql944ConnectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Database connection test: SQL944 connection successful");
                    }
                }

                // ตรวจสอบการเชื่อมต่อ CES941
                if (!string.IsNullOrEmpty(_ces941ConnectionString))
                {
                    using (var connection = new SqlConnection(_ces941ConnectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Database connection test: CES941 connection successful");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database connection test failed: {ex.Message}");
                return false;
            }
        }

        private async Task EnsurePermissionsLoaded()
        {
            if (CurrentPermissions == null)
            {
                _logger.LogInformation("GetEmployeesData: Permissions not loaded, loading now...");
                await LoadPermissions("Employee", "employee");
            }
        }

        private async Task<SQL944DataResult> GetSQL944Data(string department)
        {
            var sql944Employees = new HashSet<string>();
            var cardDict = new Dictionary<string, object>();
            var fingerprintDict = new Dictionary<string, object>();
            var nameDict = new Dictionary<string, string>();
            var deptNameDict = new Dictionary<string, string>();
            var deptIDDict = new Dictionary<string, string>();
            var cardNumberDict = new Dictionary<string, string>();
            var accessCountDict = new Dictionary<string, int>();
            var fingerprintDataIds = new HashSet<string>();

            using var sql944Connection = new SqlConnection(_sql944ConnectionString);
            await sql944Connection.OpenAsync();

            // รวม query ทั้งหมดเป็น query เดียวเพื่อลดการ round-trip ไปยังฐานข้อมูล // 合并所有查询为单个查询以减少数据库往返
            var combinedSql = @"
                WITH PersonData AS (
                    SELECT 
                        personID, Name, DeptName, DeptID, CardNumber, note,
                        CASE 
                            WHEN personID LIKE '%-1' THEN 'card'
                            WHEN personID LIKE '%-2' THEN 'fingerprint'
                        END as personType
                    FROM Person 
                    WHERE personID LIKE '%-1' OR personID LIKE '%-2'
                ),
                BioData AS (
                    SELECT PersonID, Result, GetBioDate 
                    FROM BioDataGetList 
                    WHERE PersonID LIKE '%-2'
                ),
                PersonFP AS (
                    SELECT PersonID, FP1, FP2, FV1, FV2, FACE_FEA, COALESCE(TRY_CAST(IsDelete AS INT), 0) AS IsDelete
                    FROM Person_FP
                    WHERE PersonID LIKE '%-2' AND (IsDelete IS NULL OR TRY_CAST(IsDelete AS INT) = 0)
                ),
                DoorAccess AS (
                    SELECT personID, COUNT(*) as accessCount
                    FROM PubDoorAuth 
                    WHERE TRY_CAST(reserve1 AS INT) IS NULL OR TRY_CAST(reserve1 AS INT) != 2
                    GROUP BY personID
                )
                SELECT 
                    p.personID,
                    p.Name,
                    p.DeptName,
                    p.DeptID,
                    p.CardNumber,
                    p.note,
                    p.personType,
                    b.Result as BioResult,
                    b.GetBioDate,
                    pf.FP1, pf.FP2, pf.FV1, pf.FV2, pf.FACE_FEA, pf.IsDelete,
                    da.accessCount
                FROM PersonData p
                LEFT JOIN BioData b ON p.personID = b.PersonID
                LEFT JOIN PersonFP pf ON p.personID = pf.PersonID
                LEFT JOIN DoorAccess da ON p.personID = da.personID";

            var combinedData = await sql944Connection.QueryAsync<dynamic>(combinedSql);

            // ประมวลผลข้อมูลที่รวมแล้ว // 处理合并后的数据
            foreach (var row in combinedData)
            {
                string baseId = row.personID.Split('-')[0].Trim();
                string personType = row.personType?.ToString() ?? "";
                
                sql944Employees.Add(baseId);

                if (personType == "card")
                {
                    cardDict[baseId] = row;
                    
                    if (!string.IsNullOrEmpty(row.Name))
                        nameDict[baseId] = row.Name;
                    if (!string.IsNullOrEmpty(row.DeptName))
                        deptNameDict[baseId] = row.DeptName;
                    if (!string.IsNullOrEmpty(row.DeptID))
                        deptIDDict[baseId] = row.DeptID;
                    if (!string.IsNullOrEmpty(row.CardNumber))
                        cardNumberDict[baseId] = row.CardNumber;
                }
                else if (personType == "fingerprint")
                {
                    fingerprintDict[baseId] = row;
                    
                    if (!string.IsNullOrEmpty(row.Name) && !nameDict.ContainsKey(baseId))
                        nameDict[baseId] = row.Name;
                    if (!string.IsNullOrEmpty(row.DeptName) && !deptNameDict.ContainsKey(baseId))
                        deptNameDict[baseId] = row.DeptName;
                    if (!string.IsNullOrEmpty(row.DeptID) && !deptIDDict.ContainsKey(baseId))
                        deptIDDict[baseId] = row.DeptID;
                }

                // นับจำนวนสิทธิ์การเข้าประตู // 计算门禁权限数量
                if (row.accessCount != null)
                {
                    if (int.TryParse(row.accessCount.ToString(), out int accessCount))
                    {
                        accessCountDict[baseId] = accessCount;
                    }
                    else
                    {
                        accessCountDict[baseId] = 0;
                    }
                }

                // ตรวจสอบข้อมูลลายนิ้วมือ // 检查指纹数据
                if (personType == "fingerprint")
                {
                    bool hasFingerprintData = (
                        (row.FP1 != null && row.FP1.ToString() != string.Empty) ||
                        (row.FP2 != null && row.FP2.ToString() != string.Empty) ||
                        (row.FV1 != null && row.FV1.ToString() != string.Empty) ||
                        (row.FV2 != null && row.FV2.ToString() != string.Empty) ||
                        (row.FACE_FEA != null && row.FACE_FEA.ToString() != string.Empty) ||
                        (row.BioResult != null && 
                         string.Equals(row.BioResult.ToString(), "Fingerprint data archiving:True", StringComparison.Ordinal))
                    );

                    if (hasFingerprintData)
                    {
                        fingerprintDataIds.Add(baseId);
                    }
                }
            }

            return new SQL944DataResult
            {
                sql944Employees = sql944Employees,
                cardDict = cardDict,
                fingerprintDict = fingerprintDict,
                nameDict = nameDict,
                deptNameDict = deptNameDict,
                deptIDDict = deptIDDict,
                cardNumberDict = cardNumberDict,
                accessCountDict = accessCountDict,
                fingerprintDataIds = fingerprintDataIds,
            };
        }

        private async Task<Dictionary<string, string>> GetShiftsData()
        {
            return await _context
                .emp_person_shift.Where(s => s.personID != null)
                .GroupBy(s => s.personID)
                .Select(g => new
                {
                    PersonID = g.Key,
                    ShiftMent = g.OrderByDescending(s => s.date).FirstOrDefault().shiftMent,
                })
                .ToDictionaryAsync(k => k.PersonID, v => v.ShiftMent);
        }

        private async Task<Dictionary<string, string>> GetJobGrades()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    string sql = @"SELECT EmpNo, JGCode FROM TEmpComDta";
                    var rows = await db.QueryAsync(sql);

                    var dict = new Dictionary<string, string>();
                    foreach (var row in rows)
                    {
                        var empNo = row.EmpNo?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(empNo))
                        {
                            dict[empNo] = row.JGCode?.ToString()?.Trim();
                        }
                    }
                    return dict;
                }
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        // ฟังก์ชั่นดึงข้อมูล training status สำหรับพนักงาน (รองรับหลายรายการต่อคน)
        // 获取员工培训状态数据（支持每个员工多条记录）
        private async Task<Dictionary<string, object>> GetTrainingStatusData()
        {
            var trainingDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var oracleConnection = new SqlConnection(_oracleConnectionString))
                {
                    var sql = @"
                        SELECT 
                            esmms_exam_employee.id, 
                            esmms_exam_employee.emp_no, 
                            esmms_exam_employee.course_id, 
                            esmms_exam_employee.pd_line, 
                            esmms_exam_employee.score, 
                            esmms_exam_employee.exam_id, 
                            esmms_exam_employee.expire_period_snapshot, 
                            esmms_exam_employee.expire_at, 
                            esmms_exam_employee.is_active, 
                            esmms_exam_employee.assigned_at, 
                            esmms_exam_employee.created_at, 
                            esmms_exam_employee.updated_at, 
                            esmms_exam_employee.created_by, 
                            esmms_exam_employee.updated_by, 
                            esmms_exam_employee.pass_score, 
                            esmms_exam_employee.is_passed, 
                            esmms_exam.title as exam_title, 
                            esmms_exam.description, 
                            esmms_exam.random_question_count, 
                            esmms_exam.duration_minutes, 
                            esmms_exam.start_at, 
                            esmms_exam.end_at, 
                            esmms_exam.attempts_limit, 
                            esmms_exam.target_group, 
                            esmms_exam.excel_path, 
                            esmms_exam.forms_url,
                            esmms_course.course_code,
                            esmms_course.description_en
                        FROM esmms_exam_employee 
                        INNER JOIN esmms_exam 
                            ON esmms_exam_employee.exam_id = esmms_exam.id
                        INNER JOIN esmms_course
                            ON esmms_exam_employee.course_id = esmms_course.id
                    ";
                    var results = await connection.QueryAsync<dynamic>(sql);

                    // รวมเป็นลิสต์ต่อ emp_no และคำนวณรายการล่าสุดสำหรับ __trainingStatus
                    var temp = new Dictionary<string, List<dynamic>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var r in results)
                    {
                        string empNo = (r.emp_no ?? r.EmpNo ?? "").ToString().Trim();
                        if (string.IsNullOrEmpty(empNo)) continue;
                        if (!temp.TryGetValue(empNo, out var list))
                        {
                            list = new List<dynamic>();
                            temp[empNo] = list;
                        }

                        // คำนวณสถานะการฝึกอบรมจากข้อมูลวันที่หมดอายุ/คะแนน
                        DateTime? expireAt = null;
                        try { expireAt = (DateTime?)r.expire_at; } catch { }
                        DateTime? assignedAt = null;
                        try { assignedAt = (DateTime?)r.assigned_at; } catch { }
                        int? score = null; try { score = (int?)r.score; } catch { }
                        int? passScore = null; try { passScore = (int?)r.pass_score; } catch { }
                        bool? isPassed = null; try { isPassed = (bool?)r.is_passed; } catch { }
                        string examTitle = null; try { examTitle = (string?)r.exam_title; } catch { }
                        string courseCode = null; try { courseCode = (string?)r.course_code; } catch { }
                        string courseName = null; try { courseName = (string?)r.description_en; } catch { }

                        if (isPassed == null && score != null && passScore != null)
                        {
                            isPassed = score >= passScore;
                        }

                        string status;
                        var now = DateTime.Now;
                        if (expireAt == null)
                        {
                            status = assignedAt != null ? "blue" : "red"; // กำลังดำเนินการ หรือ ยังไม่สอบ
                        }
                        else if (expireAt.Value < now)
                        {
                            status = "red"; // หมดอายุ
                        }
                        else if ((expireAt.Value - now).TotalDays <= 30)
                        {
                            status = "yellow"; // ใกล้หมดอายุ
                        }
                        else
                        {
                            status = (isPassed == false) ? "blue" : "green"; // ไม่ผ่าน/กำลังดำเนินการ หรือ ปกติ
                        }

                        var projected = new
                        {
                            emp_no = empNo,
                            course_id = (string?)((r.course_id ?? "").ToString()),
                            pd_line = (string?)((r.pd_line ?? "").ToString()),
                            score = score,
                            pass_score = passScore,
                            is_passed = isPassed,
                            assigned_at = assignedAt,
                            end_at = expireAt, // ชื่อที่ frontend ใช้
                            expire_at = expireAt,
                            exam_title = examTitle,
                            course_code = courseCode,
                            course_name = courseName,
                            status = status
                        };

                        list.Add(projected);
                    }

                    foreach (var kv in temp)
                    {
                        // sort โดยใช้ end_at ล่าสุดก่อน ถ้าไม่มีใช้ assigned_at
                        var all = kv.Value
                            .OrderByDescending(x => (DateTime?)x.end_at ?? (DateTime?)x.assigned_at)
                            .ToList();

                        var latest = all.FirstOrDefault();

                        var payload = new Dictionary<string, object>
                        {
                            ["trainingStatuses"] = all,
                            ["__trainingStatus"] = latest ?? new { status = "red" }
                        };
                        trainingDict[kv.Key] = payload;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting training status data: {ex.Message}");
            }
            return trainingDict;
        }

        private async Task<List<dynamic>> BuildEmployeesList(
            Dictionary<string, object> ces941Dict,
            HashSet<string> sql944Employees,
            Dictionary<string, object> cardDict,
            Dictionary<string, object> fingerprintDict,
            Dictionary<string, string> nameDict,
            Dictionary<string, string> deptNameDict,
            Dictionary<string, string> deptIDDict,
            Dictionary<string, string> cardNumberDict,
            Dictionary<string, int> accessCountDict,
            HashSet<string> fingerprintDataIds,
            Dictionary<string, string> shifts,
            Dictionary<string, string> jobGradeDict,
            Dictionary<string, object> trainingStatusDict
        )
        {
            // ใช้ HashSet เพื่อลดการค้นหา O(1) // 使用HashSet以减少查找时间O(1)
            var processedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var employeesList = new List<dynamic>();

            // รวม employee IDs ทั้งหมดเพื่อประมวลผลแบบ batch // 合并所有员工ID以批量处理
            var allEmployeeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allEmployeeIds.UnionWith(ces941Dict.Keys);
            allEmployeeIds.UnionWith(sql944Employees);

            // ประมวลผลพนักงานจาก CES941 ก่อน // 先处理来自CES941的员工
            foreach (var id in allEmployeeIds)
            {
                if (processedIds.Contains(id)) continue;

                object emp = null;
                bool existsInCES941 = false;
                bool existsInSQL944 = sql944Employees.Contains(id);

                if (ces941Dict.TryGetValue(id, out emp))
                {
                    existsInCES941 = true;
                    processedIds.Add(id);
                }

                var employeeData = CreateEmployeeData(
                    id,
                    emp,
                    sql944Employees,
                    cardDict,
                    fingerprintDict,
                    nameDict,
                    deptNameDict,
                    deptIDDict,
                    cardNumberDict,
                    accessCountDict,
                    fingerprintDataIds,
                    shifts,
                    jobGradeDict,
                    trainingStatusDict,
                    existsInCES941,
                    existsInSQL944
                );

                if (employeeData != null && !string.IsNullOrWhiteSpace(employeeData.name))
                {
                    employeesList.Add(employeeData);
                }
            }

            return employeesList;
        }

        private dynamic CreateEmployeeData(
            string id,
            dynamic emp,
            HashSet<string> sql944Employees,
            Dictionary<string, dynamic> cardDict,
            Dictionary<string, dynamic> fingerprintDict,
            Dictionary<string, string> nameDict,
            Dictionary<string, string> deptNameDict,
            Dictionary<string, string> deptIDDict,
            Dictionary<string, string> cardNumberDict,
            Dictionary<string, int> accessCountDict,
            HashSet<string> fingerprintDataIds,
            Dictionary<string, string> shifts,
            Dictionary<string, string> jobGradeDict,
            Dictionary<string, object> trainingStatusDict,
            bool existsInCES941,
            bool existsInSQL944
        )
        {
            bool inSql944 = sql944Employees.Contains(id);
 
            // ตรวจสอบสิทธิ์การเข้าถึงข้อมูลพนักงานตามแผนก
            string employeeDeptId =
                deptIDDict.ContainsKey(id) && !string.IsNullOrWhiteSpace(deptIDDict[id])
                    ? deptIDDict[id]
                    : emp?.WorkArea ?? "";

            // ดึงข้อมูล result จาก fingerprintDict
            string fingerprintResult = "";
            string getBioDate = "";
            if (fingerprintDict.ContainsKey(id))
            {
                var fpData = fingerprintDict[id] as IDictionary<string, object>;
                if (fpData != null)
                {
                    if (fpData.ContainsKey("BioResult"))
                    {
                        fingerprintResult = fpData["BioResult"]?.ToString() ?? "";
                    }
                    if (fpData.ContainsKey("GetBioDate"))
                    {
                        getBioDate = fpData["GetBioDate"]?.ToString() ?? "";
                    }
                }
            }

            // ตรวจสอบ note status
            string noteStatus = "";
            if (cardDict.ContainsKey(id))
            {
                var cardData = cardDict[id] as IDictionary<string, object>;
                if (cardData != null && cardData.ContainsKey("note"))
                {
                    string noteValue = cardData["note"]?.ToString()?.Trim() ?? "";
                    if (noteValue.Equals("del", StringComparison.OrdinalIgnoreCase))
                    {
                        noteStatus = "ข้อมูลรอลบ";
                    }
                }
            }

            // ดึงและปรับรูปแบบ Job Grade โดยคง suffix 'A' ถ้ามี และตัด prefix 'JG' และศูนย์นำหน้าเลข (เช่น JG06 -> 6, JG04A -> 4A)
            string jobGradeValue = "";
			if (jobGradeDict != null && jobGradeDict.TryGetValue(id, out string rawJobGrade) && !string.IsNullOrWhiteSpace(rawJobGrade))
            {
                var s = rawJobGrade.ToString().Trim().ToUpperInvariant();
                if (s.StartsWith("JG"))
                {
                    s = s.Substring(2);
                }
                bool hasA = s.EndsWith("A");
                var core = hasA ? s.Substring(0, s.Length - 1) : s;
                core = (core ?? string.Empty).TrimStart('0');
                if (string.IsNullOrEmpty(core))
                {
                    core = "0";
                }
                jobGradeValue = hasA ? core + "A" : core;
            }

            // ดึงข้อมูล training status หลายรายการและตัวล่าสุด // 获取多条及最新培训状态
			object trainingData = null; // โครงรวม { trainingStatuses, __trainingStatus }
			object trainingLatest = null; // ตัวล่าสุดสำหรับเรนเดอร์เร็ว
			object trainingList = null; // รายการทั้งหมดของพนักงานคนนี้
			if (trainingStatusDict != null && trainingStatusDict.TryGetValue(id, out object training))
            {
                trainingData = training;
                try
                {
                    var dict = training as IDictionary<string, object>;
                    if (dict != null && dict.ContainsKey("__trainingStatus"))
                    {
                        trainingLatest = dict["__trainingStatus"];
                        if (dict.ContainsKey("trainingStatuses"))
                        {
                            trainingList = dict["trainingStatuses"];
                        }
                    }
                    else
                    {
                        trainingLatest = training; // fallback
                    }
                }
                catch { trainingLatest = training; }
            }

            // สร้างข้อมูลพนักงาน
            return new
            {
                personID = id,
                name = GetEmployeeName(id, emp, nameDict, cardDict),
                department = GetEmployeeDepartment(id, emp, deptNameDict),
                deptID = GetEmployeeDeptID(id, emp, deptIDDict),
                position = emp?.PositionName ?? "",
                startDate = emp?.EmpStartDate,
                resignDate = emp?.EmpResignDate,
                phone = emp?.MobilePhone ?? "",
                email = emp?.eMailAdd ?? "",
                gender = emp?.Gender ?? "",
                cardNumber = cardNumberDict.ContainsKey(id) ? cardNumberDict[id] : "",
                hasCardData = cardDict.ContainsKey(id),
                hasFingerprintData = fingerprintDict.ContainsKey(id),
                fingerprintResult = fingerprintResult,
                getBioDate = getBioDate,
                accessCount = accessCountDict.ContainsKey(id) ? accessCountDict[id] : 0,
                shift = shifts.ContainsKey(id) ? shifts[id] : "",
                jobGrade = jobGradeValue,
                hasFingerprint = fingerprintDataIds.Contains(id),
                isActive = (cardDict.ContainsKey(id) || fingerprintDict.ContainsKey(id)) ? 1 : 0,
                existsInCES941 = existsInCES941,
                existsInSQL944 = existsInSQL944,
                noteStatus = noteStatus, // เพิ่มข้อมูลสถานะ note,
                WorkArea = emp?.WorkArea ?? "",
                Wrkplanid = emp?.WrkPlanID ?? "",
                trainingStatuses = trainingList, // รวมทั้งหมดของคนนี้
	                __trainingStatus = trainingLatest, // ใช้ตัวล่าสุดสำหรับคอลัมน์สรุป
            };
        }

        private string GetEmployeeName(
            string id,
            dynamic emp,
            Dictionary<string, string> nameDict,
            Dictionary<string, dynamic> cardDict
        )
        {
            // ใช้ชื่อจาก SQL944 ก่อน หากไม่เจอให้ใช้ชื่อจาก CES941
            // 优先使用 SQL944 中的姓名，如果找不到则使用 CES941 中的姓名
            if (nameDict.ContainsKey(id) && !string.IsNullOrWhiteSpace(nameDict[id]))
            {
                return nameDict[id];
            }

            // ตรวจสอบ emp ก่อนเรียกใช้ EmpNo
            // 检查 emp 后再使用 EmpNo
            //if (emp.EmpNo.ToString().Contains("10129826"))
            //{
            //    Console.WriteLine("just test");
            //}

            // ใช้ชื่อจาก CES941 เป็น fallback
            // 使用 CES941 中的姓名作为备选
            if (emp != null && !string.IsNullOrWhiteSpace(emp.EmpName))
            {
                // สร้างชื่อเต็มจากข้อมูล CES941
                // 从 CES941 数据创建全名
                string fullName = "";
                if (!string.IsNullOrWhiteSpace(emp.PrefixName) && !string.IsNullOrWhiteSpace(emp.EmpName) && !string.IsNullOrWhiteSpace(emp.EmpLName))
                {
                    fullName = $"{emp.PrefixName} {emp.EmpName} {emp.EmpLName}";
                }
                else if (!string.IsNullOrWhiteSpace(emp.EmpName) && !string.IsNullOrWhiteSpace(emp.EmpLName))
                {
                    fullName = $"{emp.EmpName} {emp.EmpLName}";
                }
                else if (!string.IsNullOrWhiteSpace(emp.FullName))
                {
                    fullName = emp.FullName;
                }

                return fullName;
            }

            return "ไม่ระบุชื่อพนักงาน";
        }

        private string GetEmployeeDepartment(
            string id,
            dynamic emp,
            Dictionary<string, string> deptNameDict
        )
        {
            if (deptNameDict.ContainsKey(id) && !string.IsNullOrWhiteSpace(deptNameDict[id]))
            {
                return deptNameDict[id];
            }

            return emp?.DeptName ?? "";
        }

        private string GetEmployeeDeptID(
            string id,
            dynamic emp,
            Dictionary<string, string> deptIDDict
        )
        {
            if (deptIDDict.ContainsKey(id) && !string.IsNullOrWhiteSpace(deptIDDict[id]))
            {
                return deptIDDict[id];
            }

            return emp?.DeptID ?? "";
        }

        private async Task<List<dynamic>> ApplyFilters(
            List<dynamic> employeesList,
            string status,
            string door,
            string department,
            string shift,
            string fingerprint,
            string pdline
        )
        {
            // กรองตามสถานะ
            if (!string.IsNullOrEmpty(status))
            {
                employeesList = status switch
                {
                    "1" => employeesList
                        .Where(e => ((dynamic)e).hasCardData || ((dynamic)e).hasFingerprintData)
                        .ToList(),
                    "0" => employeesList
                        .Where(e => !((dynamic)e).hasCardData && !((dynamic)e).hasFingerprintData)
                        .ToList(),
                    "incomplete" => employeesList
                        .Where(e => !((dynamic)e).existsInCES941 || !((dynamic)e).existsInSQL944)
                        .ToList(),
                    "ces941only" => employeesList
                        .Where(e => ((dynamic)e).existsInCES941
                                    && !((dynamic)e).existsInSQL944
                                    && ((dynamic)e).resignDate == null)
                        .ToList(),
                    "sql944only" => employeesList
                        .Where(e => !((dynamic)e).existsInCES941 && ((dynamic)e).existsInSQL944)
                        .ToList(),
                    "active" => employeesList.Where(e => ((dynamic)e).resignDate == null).ToList(),
                    "resigned" => employeesList
                        .Where(e => ((dynamic)e).resignDate != null)
                        .ToList(),
                    _ => employeesList,
                };
            }

            // กรองตามแผนก
            if (!string.IsNullOrEmpty(department))
            {
                var selectedDepartments = department.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();
                
                if (selectedDepartments.Any())
                {
                    employeesList = employeesList.Where(e =>
                    {
                        var emp = (dynamic)e;
                        string workArea = HasProp(emp, "WorkArea") && emp.WorkArea != null
                            ? emp.WorkArea.ToString().Trim()
                            : string.Empty;
                        string deptID = HasProp(emp, "DeptID") && emp.DeptID != null
                            ? emp.DeptID.ToString().Trim()
                            : string.Empty;
                        string deptName = HasProp(emp, "DeptName") && emp.DeptName != null
                            ? emp.DeptName.ToString().Trim()
                            : string.Empty;

                        
                        // ตรวจสอบว่าตรงกับแผนกที่เลือกหรือไม่
                        return selectedDepartments.Any(selectedDept => 
                            workArea.Equals(selectedDept, StringComparison.OrdinalIgnoreCase) ||
                            deptID.Equals(selectedDept, StringComparison.OrdinalIgnoreCase) ||
                            deptName.Equals(selectedDept, StringComparison.OrdinalIgnoreCase));
                    }).ToList();
                }
            }

            // กรองตามไลน์ผลิตจากตารางการสอบ (esmms_exam_employee) โดยรองรับค่าที่ส่งมาเป็นรหัสหรือชื่อ (จาก sys_pdline)
            if (!string.IsNullOrWhiteSpace(pdline))
            {
                var tokens = pdline
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (tokens.Any())
                {
                    var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var t in tokens)
                    {
                        // ถ้าเป็นตัวเลขให้ถือเป็นไอดีด้วย
						if (int.TryParse(t, out int __)) selectedIds.Add(t);
                        selectedNames.Add(t);
                    }

                    // แม็ปชื่อ -> ไอดี จาก sys_pdline เพื่อรองรับกรณี UI ส่งชื่อมา
                    try
                    {
                        using (var conn = new SqlConnection(_connectionString))
                        {
                            await conn.OpenAsync();
                            var rows = await conn.QueryAsync(
                                @"SELECT PDLINE_ID, PDLINE_NAME FROM sys_pdline WHERE PDLINE_NAME IN @Names OR PDLINE_ID IN @Ids",
                                new { Names = selectedNames.ToList(), Ids = selectedIds.ToList() }
                            );
                            foreach (var r in rows)
                            {
                                var idStr = (r.PDLINE_ID?.ToString() ?? string.Empty).Trim();
                                var nmStr = (r.PDLINE_NAME?.ToString() ?? string.Empty).Trim();
                                if (!string.IsNullOrEmpty(idStr)) selectedIds.Add(idStr);
                                if (!string.IsNullOrEmpty(nmStr)) selectedNames.Add(nmStr);
                            }
                        }
                    }
                    catch { /* ถ้าแม็ปไม่ได้ ให้ใช้ค่าที่ส่งมาโดยตรง */ }

                    // ดึงพนักงานจากตารางการสอบที่มี pd_line ตรงกับไอดีหรือชื่อที่เลือก
                    var matchedEmpNos = await _context.esmms_exam_employee
                        .Where(ee => ee.pd_line != null && (
                            selectedIds.Contains(ee.pd_line) || selectedNames.Contains(ee.pd_line)
                        ))
                        .Select(ee => ee.emp_no)
                        .Distinct()
                        .ToListAsync();

                    if (matchedEmpNos.Count == 0)
                    {
                        // ไม่พบข้อมูล ตัดเป็นลิสต์ว่าง
                        employeesList = new List<dynamic>();
                    }
                    else
                    {
                        var empSet = new HashSet<string>(matchedEmpNos, StringComparer.OrdinalIgnoreCase);
                        employeesList = employeesList.Where(e =>
                        {
                            var emp = (dynamic)e;
                            var pid = HasProp(emp, "personID") && emp.personID != null ? emp.personID.ToString().Trim() : string.Empty;
                            return empSet.Contains(pid);
                        }).ToList();
                    }
                }
            }

            // กรองตามประตู
            if (!string.IsNullOrEmpty(door) && door != "all")
            {
                employeesList = await FilterByDoor(employeesList, door);
            }

        
            // กรองตามกะ - รองรับทั้งค่าจาก emp_person_shift (shift) และ WrkPlanID (จาก CES941)
            if (!string.IsNullOrEmpty(shift) && shift != "all")
            {
                employeesList = employeesList
                    .Where(e =>
                    {
                        var emp = (dynamic)e;
                        string empShift = HasProp(emp, "shift") && emp.shift != null
                            ? emp.shift.ToString().ToLower().Trim()
                            : string.Empty;
                        string empWrkPlanId = HasProp(emp, "Wrkplanid") && emp.Wrkplanid != null
                            ? emp.Wrkplanid.ToString().ToLower().Trim()
                            : (HasProp(emp, "WrkPlanID") && emp.WrkPlanID != null
                                ? emp.WrkPlanID.ToString().ToLower().Trim()
                                : string.Empty);

                        string filterShift = shift.ToLower().Trim();

                        // ตรงกับ WrkPlanID เป็นหลัก และสำรองด้วย shift (ในกรณีใช้ข้อมูลในระบบภายใน)
                        bool matchWrkPlan = !string.IsNullOrEmpty(empWrkPlanId) &&
                                            (empWrkPlanId == filterShift || empWrkPlanId.Contains(filterShift));
                        bool matchShift = !string.IsNullOrEmpty(empShift) &&
                                          (empShift == filterShift || empShift.Contains(filterShift));
                        return matchWrkPlan || matchShift;
                    })
                    .ToList();
            }

            // กรองตามข้อมูลลายนิ้วมือ
            if (!string.IsNullOrEmpty(fingerprint))
            {
                employeesList = fingerprint switch
                {
                    "has" => employeesList.Where(e => ((dynamic)e).hasFingerprint).ToList(),
                    "none" => employeesList.Where(e => !((dynamic)e).hasFingerprint).ToList(),
                    _ => employeesList,
                };
            }


            return employeesList;
        }

        // helper สำหรับตรวจสอบ property บน dynamic แบบปลอดภัย
        private static bool HasProp(dynamic obj, string name)
        {
            if (obj is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(name);
            }
            try
            {
                return obj.GetType().GetProperty(name) != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<dynamic>> FilterByDoor(List<dynamic> employeesList, string door)
        {
            using var sql944Connection = new SqlConnection(_sql944ConnectionString);
            var doorAccessSql =
                @"SELECT DISTINCT personID FROM PubDoorAuth WHERE doorID = @doorID AND (TRY_CAST(reserve1 AS INT) IS NULL OR TRY_CAST(reserve1 AS INT) != 2)";
            var authorizedPersons = await sql944Connection.QueryAsync<string>(
                doorAccessSql,
                new { doorID = door }
            );
            var basePersonIds = authorizedPersons.Select(p => p.Split('-')[0].Trim()).ToHashSet();

            return employeesList.Where(e => basePersonIds.Contains(((dynamic)e).personID)).ToList();
        }

        private void LogEmployeeCount(int count)
        {
            _logger.LogInformation($"GetEmployeesData: Final employee list has {count} employees");
            if (count == 0)
            {
                _logger.LogWarning(
                    "GetEmployeesData: Warning - No employees found. This might be due to permission filtering."
                );
            }
        }

        // เพิ่มเมธอดใหม่สำหรับดึงข้อมูล access
        private async Task<List<dynamic>> GetAccessData()
        {
            using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
            {
                string sql =
                    @"
                    SELECT 
                        p.PersonID,
                        pd.doorID,
                        pd.doorName, 
                        pd.contolL1,
                        pw.weekTimeID
                    FROM Person p
                    CROSS JOIN PubDoor pd
                    LEFT JOIN PubWeekTime pw ON pd.contolL1 = pw.contolL1";

                var result = await db.QueryAsync(sql);
                return result.ToList();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncEmployee()
        {
            try
            {
                using (IDbConnection db944 = new SqlConnection(_sql944ConnectionString))
                using (IDbConnection dbMain = new SqlConnection(_connectionString))
                {
                    // ดะบุ column ที่ต้องการและจัดการ format วันที่
                    var sql944Query =
                        @"
                        SELECT 
                            CardNumber, PersonID, Name, DeptID, DeptName,
                            CONVERT(datetime, LeaveJobDate, 120) as LeaveJobDate,
                            CONVERT(datetime, EnableDate, 120) as EnableDate,
                            CONVERT(datetime, DisableDate, 120) as DisableDate,
                            UserLevel, Password, SuperPassword,
                            CONVERT(datetime, ModifyTime, 120) as ModifyTime,
                            Reserve1, Reserve2, Reserve3, Reserve4,
                            ReserveChar1, CardType, CardTypeDesc,
                            SubSystem, UseCategory, UseStatus, Status,
                            CardCategory, CardStatus,
                            CONVERT(datetime, CREATEDATE, 120) as CREATEDATE
                        FROM Person";

                    var currentQuery =
                        @"
                        SELECT 
                            Id, CardNumber, PersonID, Name, DeptID, DeptName,
                            LeaveJobDate, EnableDate, DisableDate,
                            UserLevel, Password, SuperPassword, ModifyTime,
                            Reserve1, Reserve2, Reserve3, Reserve4,
                            ReserveChar1, CardType, CardTypeDesc,
                            SubSystem, UseCategory, UseStatus, Status,
                            CardCategory, CardStatus, CREATEDATE,
                            isActive, RecordStatus
                        FROM emp_person";

                    var sql944Data = await db944.QueryAsync<emp_person>(sql944Query);
                    var currentData = await dbMain.QueryAsync<emp_person>(currentQuery);

                    var sql944List = sql944Data.ToList();
                    var currentList = currentData.ToList();

                    var newEmployees = new List<emp_person>();
                    var updatedEmployees = new List<emp_person>();

                    // สร้างรายการ personID จาก sql944List
                    var sql944PersonIDs = sql944List.Select(e => e.personID).ToHashSet();

                    foreach (var target in currentList)
                    {
                        var source = sql944List.FirstOrDefault(e => e.personID == target.personID);
                        if (source == null)
                        {
                            // ถ้ไม่มีใน sql944Query ให้ตั้งค่า isActive เป็น 0
                            target.isActive = 0;
                            updatedEmployees.Add(target);
                        }
                        else
                        {
                            // ถ้าีใน sql944Query ให้คัดลอกคุณสมบัติ
                            CopyProperties(source, target);
                            updatedEmployees.Add(target);
                        }
                    }

                    // เพิ่มพนักงานใหม่ที่ไม่มีใน currentList
                    foreach (var source in sql944List)
                    {
                        if (!currentList.Any(e => e.personID == source.personID))
                        {
                            source.isActive = 1; // ตั้งค่า isActive เป็น 1 สำหรับนักงานใหม่
                            newEmployees.Add(source);
                        }
                    }

                    if (newEmployees.Any())
                    {
                        await _context.emp_person.AddRangeAsync(newEmployees);
                    }

                    if (updatedEmployees.Any())
                    {
                        _context.emp_person.UpdateRange(updatedEmployees);
                    }

                    await _context.SaveChangesAsync();

                    return Json(
                        new
                        {
                            success = true,
                            message = "ซิงคข้อมูลสำเร็จ",
                            newRecords = newEmployees.Count,
                            updatedRecords = updatedEmployees.Count,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = $"เกิดข้อผิดพลาด: {ex.Message}",
                        stackTrace = ex.StackTrace,
                    }
                );
            }
        }

        // Helper method สำหรับ copy properties
        private void CopyProperties(emp_person source, emp_person target)
        {
            var properties = typeof(emp_person)
                .GetProperties()
                .Where(p => p.Name != "Id" && p.CanWrite); // ไม่ copy Id

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source);
                if (value != null) // copy เฉพาะค่าที่ไม่ใช null
                {
                    prop.SetValue(target, value);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeById(string personID)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var sql944 = new SqlConnection(_sql944ConnectionString))
                {
                    // ตรวจสอบว่ามีการส่ง personID มาหรือไม่
                    if (string.IsNullOrEmpty(personID))
                    {
                        return Json(new { success = false, message = "กรุณาระบุรหัสพนักงาน" });
                    }

                    // ดึงข้อมูลจาก Person ใน SQL944
                    var sql944Employee = await sql944.QueryAsync<emp_person>(
                        @"SELECT * FROM Person 
                          WHERE personID LIKE @personID + '%'",
                        new { personID }
                    );

                    // ตรวจสอบว่าพบข้อมูลหรือไม่
                    if (sql944Employee == null || !sql944Employee.Any())
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // ตรวจสอบสิทธิ์การเข้าถึงข้อมูลพนักงานตามแผนก
                    var firstEmployee = sql944Employee.FirstOrDefault();

                    var result = new { success = true, data = new { emp_person = sql944Employee } };

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetEmployeeById: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("Employee/AddEmployee")]
        public async Task<IActionResult> AddEmployee(
            string personID,
            string cardNumber,
            string name,
            string deptID,
            string deptName
        )
        {
            try
            {
                // ตรวจสอบข้อมูลที่จำเป็น
                if (
                    string.IsNullOrEmpty(personID)
                    || string.IsNullOrEmpty(cardNumber)
                    || string.IsNullOrEmpty(name)
                )
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ตรวจสอบการซ้ำของเลขบัตร
                    var existingCard = await sql944Connection.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 1 FROM Person WHERE CardNumber = @cardNumber",
                        new { cardNumber }
                    );

                    if (existingCard.HasValue)
                    {
                        return Json(
                            new { success = false, message = "เลขบัตรนี้มีอยู่ในระบบแล้ว" }
                        );
                    }

                    // สร้างข้อมูลพนักงานสำหรับบัตรและลายนิ้วมือ
                    var employees = new[]
                    {
                        new emp_person
                        {
                            cardNumber = cardNumber,
                            personID = $"{personID}-1",
                            name = name,
                            deptID = deptID,
                            deptName = deptName,
                            CREATEDATE = DateTime.Now,
                            isActive = 1,
                            RecordStatus = "N",
                            status = "1",
                            useCategory = "Normal card",
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            userLevel = "29991231",
                            reserve3 = cardNumber,
                            reserveChar1 = "19991231",
                            cardType = "1",
                            cardTypeDesc = "1",
                            subSystem = "1",
                            useStatus = "1",
                            cardCategory = "1",
                            cardStatus = "1",
                            modifyTime = DateTime.Now,
                            reserve1 = "1",
                            reserve2 = "1",
                            reserve4 = "1",
                        },
                        new emp_person
                        {
                            cardNumber = personID,
                            personID = $"{personID}-2",
                            name = name,
                            deptID = deptID,
                            deptName = deptName,
                            CREATEDATE = DateTime.Now,
                            isActive = 1,
                            RecordStatus = "N",
                            userLevel = "29991231",
                            status = "1",
                            useCategory = "Normal card",
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                            reserve3 = personID,
                            reserveChar1 = "19991231",
                            cardType = "1",
                            cardTypeDesc = "1",
                            subSystem = "1",
                            useStatus = "1",
                            cardCategory = "1",
                            cardStatus = "1",
                            modifyTime = DateTime.Now,
                            reserve1 = "1",
                            reserve2 = "1",
                            reserve4 = "1",
                        },
                    };

                    // SQL สำหรับเพิ่มข้อมูล
                    var sql =
                        @"
                        INSERT INTO Person (
                            CardNumber, PersonID, Name, DeptID, DeptName,
                            UserLevel, Status, ReserveChar1, CardType, CardTypeDesc,
                            SubSystem, UseCategory, UseStatus, CardCategory, CardStatus,
                            reserve3, modifyTime, leaveJobDate, enableDate, disableDate
                        )
                        VALUES (
                            @CardNumber, @PersonID, @Name, @DeptID, @DeptName,
                            @UserLevel, @Status, @ReserveChar1, @CardType, @CardTypeDesc,
                            @SubSystem, @UseCategory, @UseStatus, @CardCategory, @CardStatus,
                            @reserve3, @modifyTime, @leaveJobDate, @enableDate, @disableDate
                        )";

                    // บันทึกข้อมูลทั้งสองรายการ
                    foreach (var employee in employees)
                    {
                        await sql944Connection.ExecuteAsync(sql, MapEmployeeToSQL944(employee));
                    }

                    return Json(new { success = true, message = "เพิ่มข้อมูลพนักงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddEmployee: {ex.Message}");
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการเพิ่มข้อมูลพนักงาน" }
                );
            }
        }

        private emp_person CloneEmployee(emp_person source, string personId)
        {
            var clone = new emp_person();
            CopyProperties(source, clone);
            clone.personID = personId;
            return clone;
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string personId, bool isActive)
        {
            try
            {
                // ดึงข้อมูลพนักงานโดยใช้ Where แทน FirstOrDefaultAsync
                var basePersonId = personId.Split('-')[0];
                var employee = await _context
                    .emp_person.Where(e => e.personID.StartsWith(basePersonId))
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                }

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                {
                    if (isActive) // เปิดใช้งาน
                    {
                        // อัพเดท SQL944
                        await sql944Connection.ExecuteAsync(
                            @"
                            UPDATE Person 
                            SET useCategory = 'Normal card'
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดท PubDoorAuth
                        await sql944Connection.ExecuteAsync(
                            @"
                            UPDATE PubDoorAuth 
                            SET reserve1 = 2
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดท HR_IPS
                        await hrConnection.ExecuteAsync(
                            @"
                            UPDATE emp_person 
                            SET useCategory = 'Normal card',
                                isActive = 1
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดทฐานข้อมูลหลัก
                        await _context.Database.ExecuteSqlRawAsync(
                            @"
                            UPDATE emp_person 
                            SET useCategory = 'Normal card',
                                isActive = 1
                            WHERE personID IN ({0}, {1})",
                            basePersonId + "-1",
                            basePersonId + "-2"
                        );
                    }
                    else // ปิดใช้งาน
                    {
                        // อัพเดท SQL944
                        await sql944Connection.ExecuteAsync(
                            @"
                            UPDATE Person 
                            SET useCategory = 'Expired card'
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดท PubDoorAuth
                        await sql944Connection.ExecuteAsync(
                            @"
                            UPDATE PubDoorAuth 
                            SET reserve1 = 2
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดท HR_IPS
                        await hrConnection.ExecuteAsync(
                            @"
                            UPDATE emp_person 
                            SET useCategory = 'Expired card',
                                isActive = 0
                            WHERE personID IN (@personId1, @personId2)",
                            new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                        );

                        // อัพเดทฐานข้อมูลหลัก
                        await _context.Database.ExecuteSqlRawAsync(
                            @"
                            UPDATE emp_person 
                            SET useCategory = 'Expired card',
                                isActive = 0
                            WHERE personID IN ({0}, {1})",
                            basePersonId + "-1",
                            basePersonId + "-2"
                        );
                    }

                    return Json(
                        new
                        {
                            success = true,
                            message = isActive ? "เปิดใช้งานสำเร็จ" : "ปิดใช้งานสำเร็จ",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpGet]
        [Route("Employee/GetEmployeeStats")]
        public async Task<IActionResult> GetEmployeeStats()
        {
            try
            {
                // ดึงข้อมูลพนักงานจาก CES941
                var ces941Employees = await GetEmployeesFromCES941(ViewData["CurrentUserDepartment"]?.ToString() ?? "");

                // ดึงข้อมูลแผนกจาก SQL944
                var sql944Departments = new Dictionary<string, string>();

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var departmentQuery =
                        @"
                        SELECT DISTINCT personID, deptID, deptName
                        FROM Person
                        WHERE deptID IS NOT NULL AND deptName IS NOT NULL";

                    var departments = await sql944Connection.QueryAsync(departmentQuery);

                    // สร้าง Dictionary เก็บข้อมูลแผนก โดยใช้ personID เป็น key
                    foreach (var dept in departments)
                    {
                        string personId = dept.personID?.ToString();
                        if (!string.IsNullOrEmpty(personId))
                        {
                            // ตัด -1 หรือ -2 ออกเพื่อให้ได้รหัสพนักงานพื้นฐาน
                            string basePersonId = personId.Split('-')[0];
                            sql944Departments[basePersonId] = dept.deptName?.ToString();
                        }
                    }
                }

                // คำนวณสถิติพื้นฐาน
                int totalEmployees = ces941Employees.Count;
                int activeEmployees = ces941Employees.Count(e => e.EmpResignDate == null);

                // คำนวณพนักงานใหม่เดือนนี้
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                int newEmployees = ces941Employees.Count(e =>
                {
                    if (e.EmpStartDate == null)
                        return false;
                    return DateTime.TryParse(e.EmpStartDate.ToString(), out DateTime startDate)
                        && startDate >= firstDayOfMonth;
                });

                // คำนวณพนักงานลาออกเดือนนี้
                int resignedEmployees = ces941Employees.Count(e =>
                {
                    if (e.EmpResignDate == null)
                        return false;
                    return DateTime.TryParse(e.EmpResignDate.ToString(), out DateTime resignDate)
                        && resignDate >= firstDayOfMonth;
                });

                // ดึงข้อมูลพนักงานที่มีวันเกิดวันนี้
                var birthdayEmployees = ces941Employees
                    .Where(e =>
                    {
                        // เฉพาะพนักงานที่ยังทำงานอยู่ (ไม่มีวันลาออก)
                        if (e.BirthDate == null || e.EmpResignDate != null)
                            return false;

                        if (!DateTime.TryParse(e.BirthDate.ToString(), out DateTime birthDate))
                            return false;
                        var birthDateThisYear = new DateTime(
                            today.Year,
                            birthDate.Month,
                            birthDate.Day
                        );

                        // ถ้าวันเกิดปีนี้ผ่านไปแล้ว ให้ใช้วันเกิดปีหน้า
                        if (birthDateThisYear < today)
                            birthDateThisYear = birthDateThisYear.AddYears(1);

                        // คำนวณจำนวนวันที่เหลือจนถึงวันเกิด
                        var daysUntilBirthday = (birthDateThisYear - today).TotalDays;

                        // แสดงวันเกิดที่ผ่านมาแล้วไม่เกิน 15 วัน หรือกำลังจะมาถึงในอีก 15 วัน
                        return daysUntilBirthday >= -15 && daysUntilBirthday <= 15;
                    })
                    .Select<dynamic, dynamic>(e =>
                    {
                        // คำนวณอายุ
                        if (!DateTime.TryParse(e.BirthDate.ToString(), out DateTime birthDate))
                            return new { name = "", age = 0, department = "" };
                        var age = today.Year - birthDate.Year;

                        // ถ้ายังไม่ถึงวันเกิดในปีนี้ ให้ลดอายุลง 1 ปี
                        if (
                            today.Month < birthDate.Month
                            || (today.Month == birthDate.Month && today.Day < birthDate.Day)
                        )
                            age--;

                        // คำนวณวันเกิดในปีนี้
                        var birthDateThisYear = new DateTime(
                            today.Year,
                            birthDate.Month,
                            birthDate.Day
                        );

                        // ถ้าวันเกิดปีนี้ผ่านไปแล้ว ให้ใช้วันเกิดปีหน้า
                        if (birthDateThisYear < today)
                            birthDateThisYear = birthDateThisYear.AddYears(1);

                        // คำนวณจำนวนวันที่เหลือจนถึงวันเกิด
                        var daysUntilBirthday = (int)(birthDateThisYear - today).TotalDays;

                        // ดึงข้อมูลแผนกจาก SQL944 ถ้ามี
                        string department = "ไม่ระบุแผนก";
                        string deptName;
                        if (sql944Departments.TryGetValue(e.EmpNo.ToString(), out deptName))
                        {
                            department = deptName;
                        }

                        return new
                        {
                            name = $"{e.PrefixName} {e.EmpName} {e.EmpLName}".Trim(),
                            department = department,
                            age = age,
                            birthDate = birthDate.ToString("dd MMM"),
                            daysUntilBirthday = daysUntilBirthday,
                            isPast = daysUntilBirthday < 0,
                        };
                    })
                    .OrderBy(e => e.daysUntilBirthday) // เรียงลำดับตามวันที่ใกล้จะถึง
                    .ToList();

                // ดึงข้อมูลพนักงานที่เริ่มงานวันนี้
                var startTodayEmployees = ces941Employees
                    .Where(e =>
                    {
                        if (e.EmpStartDate == null)
                            return false;
                        if (!DateTime.TryParse(e.EmpStartDate.ToString(), out DateTime startDate))
                            return false;
                        return startDate.Date == today.Date;
                    })
                    .Select(e =>
                    {
                        // ดึงข้อมูลแผนกจาก SQL944 ถ้ามี
                        string department = "ไม่ระบุแผนก";
                        string deptName;
                        if (sql944Departments.TryGetValue(e.EmpNo.ToString(), out deptName))
                        {
                            department = deptName;
                        }

                        return new
                        {
                            name = $"{e.PrefixName} {e.EmpName} {e.EmpLName}".Trim(),
                            department = department,
                        };
                    })
                    .ToList();

                // สร้างข้อมูลสถิติแผนก
                var departmentStats = ces941Employees
                    .GroupBy(e =>
                    {
                        // ดึงข้อมูลแผนกจาก SQL944 ถ้ามี
                        string department = "ไม่ระบุแผนก";
                        string deptName;
                        if (sql944Departments.TryGetValue(e.EmpNo.ToString(), out deptName))
                        {
                            department = deptName;
                        }
                        return department;
                    })
                    .Select(g => new
                    {
                        department = g.Key,
                        totalCount = g.Count(),
                        maleCount = g.Count(e => e.Gender?.ToString() == "M"),
                        femaleCount = g.Count(e => e.Gender?.ToString() == "F"),
                    })
                    .OrderByDescending(d => d.totalCount)
                    .ToList();

                // สร้างข้อมูลสถิติรายเดือน
                var monthlyStats = new List<object>();
                for (int i = 12; i >= 0; i--)
                {
                    var month = today.AddMonths(-i);
                    var firstDay = new DateTime(month.Year, month.Month, 1);
                    var lastDay = firstDay.AddMonths(1).AddDays(-1);

                    var newCount = ces941Employees.Count(e =>
                    {
                        if (e.EmpStartDate == null)
                            return false;
                        return DateTime.TryParse(e.EmpStartDate.ToString(), out DateTime startDate)
                            && startDate >= firstDay
                            && startDate <= lastDay;
                    });

                    var resignedCount = ces941Employees.Count(e =>
                    {
                        if (e.EmpResignDate == null)
                            return false;
                        return DateTime.TryParse(e.EmpResignDate.ToString(), out DateTime resignDate)
                            && resignDate >= firstDay
                            && resignDate <= lastDay;
                    });

                    monthlyStats.Add(
                        new
                        {
                            monthName = month.ToString("MMM yyyy"),
                            newEmployees = newCount,
                            resignedEmployees = resignedCount,
                        }
                    );
                }

                // เพิ่มการดึงข้อมูลพนักงานที่ลาออก 100 คนล่าสุด
                var recentResignedEmployees = ces941Employees
                    .Where(e =>
                    {
                        if (e.EmpResignDate == null)
                            return false;

                        if (!DateTime.TryParse(e.EmpResignDate.ToString(), out DateTime resignDate))
                            return false;
                        // ตรวจสอบว่าลาออกแล้ว และเรียงลำดับตามวันที่ลาออกล่าสุด
                        return resignDate.Date <= today.Date;
                    })
                    .Select(e =>
                    {
                        // ดึงข้อมูลแผนกจาก SQL944 ถ้ามี
                        string department = "ไม่ระบุแผนก";
                        string deptName;
                        if (sql944Departments.TryGetValue(e.EmpNo.ToString(), out deptName))
                        {
                            department = deptName;
                        }

                        // คำนวณระยะเวลาการทำงาน
                        if (!DateTime.TryParse(e.EmpStartDate.ToString(), out DateTime startDate))
                            return null;
                        if (!DateTime.TryParse(e.EmpResignDate.ToString(), out DateTime resignDate))
                            return null;
                        var workDuration = resignDate - startDate;

                        // แปลงเป็นปีและเดือน
                        int years = (int)(workDuration.TotalDays / 365.25);
                        int months = (int)((workDuration.TotalDays % 365.25) / 30.44);
                        string workPeriod =
                            years > 0
                                ? $"{years} {HttpContext.Session.GetTranslation("years")} {months} {HttpContext.Session.GetTranslation("months")}"
                                : $"{months} {HttpContext.Session.GetTranslation("months")}";

                        return new
                        {
                            name = $"{e.PrefixName} {e.EmpName} {e.EmpLName}".Trim(),
                            department = department,
                            startDate = startDate.ToString("dd MMM yyyy"),
                            resignDate = resignDate.ToString("dd MMM yyyy"),
                            workPeriod = workPeriod,
                            daysAgo = (int)(today - resignDate).TotalDays,
                            resignDateValue = resignDate, // เพิ่มข้อมูลนี้สำหรับการเรียงลำดับ
                        };
                    })
                    .OrderByDescending(e => e.resignDateValue) // เรียงลำดับตามวันที่ลาออกล่าสุด
                    .Take(100) // แสดงเฉพาะ 100 คนล่าสุด
                    .ToList();

                // เพิ่ม recentResignedEmployees ในการส่งข้อมูลกลับไปยัง View
                return Json(
                    new
                    {
                        success = true,
                        totalEmployees,
                        activeEmployees,
                        newEmployees,
                        resignedEmployees,
                        birthdayEmployees,
                        startTodayEmployees,
                        recentResignedEmployees,
                        departmentStats,
                        monthlyStats,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message,
                        stackTrace = ex.StackTrace,
                    }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAccess(string personId)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูลพนักงาน
                    string personSql =
                        @"
                        SELECT TOP 1 Name, DeptName, DeptID 
                        FROM Person 
                        WHERE PersonID LIKE @personIdPattern";

                    var personInfo = await db.QueryFirstOrDefaultAsync<dynamic>(
                        personSql,
                        new { personIdPattern = personId + "%" }
                    );

                    string employeeName = personInfo?.Name ?? personId;
                    string deptName = personInfo?.DeptName ?? "";
                    string deptID = personInfo?.DeptID ?? "";

                    // ดึงข้อมูลประตูและเวลาทำงาน
                    string doorSql =
                        @"
                         SELECT 
                             PubDoor.doorID,
                             PubDoor.doorName, 
                             PubDoor.contolL1,
                             PubWeekTime.weekTimeID
                         FROM PubDoor
                         LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1
                         WHERE PubDoor.doorID NOT IN ('0000301', '0000401', '0000501', '0000601', '0000701', '0000801', '0000901','0001501')
                         ORDER BY PubDoor.doorName";

                    var doors = await db.QueryAsync(doorSql);

                    // ตรวจสอบสิทธิ์บัตร (-1)
                    var cardAccess = await CheckAccess(personId + "-1", db);

                    // ตรวจสอบสิทธิ์ลายนิ้วมือ (-2)
                    var fingerAccess = await CheckAccess(personId + "-2", db);

                    var result = doors
                        .Select(door => new
                        {
                            doorID = door.doorID,
                            contolL1 = door.contolL1,
                            doorName = door.doorName,
                            weekTimeID = door.weekTimeID,
                            personID = personId,
                            accessCard = cardAccess.Any(a =>
                                a.doorID == door.doorID && (a.reserve1 == null || a.reserve1.ToString() != "2")
                            )
                                ? 1
                                : 0,
                            accessFinger = fingerAccess.Any(a =>
                                a.doorID == door.doorID && (a.reserve1 == null || a.reserve1.ToString() != "2")
                            )
                                ? 1
                                : 0,
                            hasCardAccess = cardAccess.Any(a =>
                                a.doorID == door.doorID && (a.reserve1 == null || a.reserve1.ToString() != "2")
                            ),
                            hasFingerprintAccess = fingerAccess.Any(a =>
                                a.doorID == door.doorID && (a.reserve1 == null || a.reserve1.ToString() != "2")
                            ),
                        })
                        .ToList();

                    return Json(
                        new
                        {
                            success = true,
                            data = result,
                            employeeName = employeeName,
                            deptName = deptName,
                            deptID = deptID,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<IEnumerable<dynamic>> CheckAccess(string personId, IDbConnection db)
        {
            string sql =
                @"
                SELECT personID, doorID, reserve1 
                FROM PubDoorAuth 
                WHERE personID = @personId";

            return await db.QueryAsync(sql, new { personId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccess(
            string personId,
            string doorId,
            int state,
            string weekTimeID = null,
            int dnfp = 0
        )
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    if (state == 0)
                    {
                        // กรณียกเลิกสิทธิ์
                        string updateSql =
                            @"
                            UPDATE PubDoorAuth 
                            SET reserve1 = 2, 
                                modifyTime = @modifyTime 
                            WHERE personID = @personId 
                            AND doorID = @doorId";

                        var result = await db.ExecuteAsync(
                            updateSql,
                            new
                            {
                                modifyTime = DateTime.Now,
                                personId,
                                doorId,
                            }
                        );

                        return Json(new { status = result > 0 ? "success" : "error" });
                    }
                    else
                    {
                        // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่
                        string checkSql =
                            @"
                            SELECT COUNT(1) 
                            FROM PubDoorAuth 
                            WHERE personID = @personId 
                            AND doorID = @doorId";

                        var exists = await db.QueryFirstOrDefaultAsync<int>(
                            checkSql,
                            new { personId, doorId }
                        );

                        if (exists > 0)
                        {
                            return Json(
                                new
                                {
                                    status = "error",
                                    message = "มีสิทธิ์การเข้าประตูนี้อยู่แล้ว",
                                }
                            );
                        }

                        // เพิ่มสิทธิ์ใหม่
                        string insertSql =
                            @"
                            INSERT INTO PubDoorAuth (
                                personID, doorID, weekTimeID, holidayGroupID,
                                tag, reserve1, reserve2, reserve3, reserve4,
                                operator, UserGroupID, modifyTime,
                                DoubleCheck, BlackList, Check_TimeZone,
                                Check_Holiday, DownLoad_Fg, contolL1_N, Master_Card
                            ) VALUES (
                                @personID, @doorID, @weekTimeID, @holidayGroupID,
                                @tag, @reserve1, @reserve2, @reserve3, @reserve4,
                                @operator, @UserGroupID, @modifyTime,
                                @DoubleCheck, @BlackList, @Check_TimeZone,
                                @Check_Holiday, @DownLoad_Fg, @contolL1_N, @Master_Card
                            )";

                        var parameters = new
                        {
                            personID = personId,
                            doorID = doorId,
                            weekTimeID = weekTimeID,
                            holidayGroupID = (string)null,
                            tag = "HTA860PMF",
                            reserve1 = 0,
                            reserve2 = (string)null,
                            reserve3 = (string)null,
                            reserve4 = (string)null,
                            @operator = "SUPERVISOR",
                            UserGroupID = (string)null,
                            modifyTime = DateTime.Now,
                            DoubleCheck = 0,
                            BlackList = 0,
                            Check_TimeZone = 1,
                            Check_Holiday = 1,
                            DownLoad_Fg = dnfp,
                            contolL1_N = (string)null,
                            Master_Card = 0,
                        };

                        var result = await db.ExecuteAsync(insertSql, parameters);
                        return Json(new { status = result > 0 ? "success" : "error" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAllAccess(string personId)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูลประตูทั้งหมด
                    var doors = await db.QueryAsync<dynamic>(
                        @"
                        SELECT 
                            PubDoor.doorID,
                            PubDoor.doorName, 
                            PubDoor.contolL1,
                            PubWeekTime.weekTimeID 
                        FROM PubDoor 
                        LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1
                        WHERE PubDoor.doorID NOT IN ('0000301', '0000401', '0000501', '0000601', '0000701', '0000801', '0000901','0001501')
                        ORDER BY PubDoor.doorName"
                    );

                    foreach (var door in doors)
                    {
                        // เพิ่มสิทธิ์สำหรับบัตร (-1)
                        await AddDoorAccess(personId + "-1", door.doorID, door.weekTimeID, 0, db);
                        // เพิ่มสิทธิ์สำหรับลายนิ้วมือ (-2)
                        await AddDoorAccess(personId + "-2", door.doorID, door.weekTimeID, 1, db);
                    }

                    return Json(new { status = "success" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddHRAccess(string personId)
        {
            try
            {
                var result = await AddBasicDoorAccess(personId);
                if (result.Success)
                {
                    return Json(new { status = "success", message = result.Message });
                }
                else
                {
                    return Json(new { status = "error", message = result.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        private async Task<AccessResult> AddBasicDoorAccess(string personId)
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ดึงข้อมูลประตูพื้นฐานที่พนักงานทุกคนควรมี
                    var basicDoors = await connection.QueryAsync<dynamic>(
                        @"
                        SELECT doorID, weekTimeID 
                        FROM PubDoor 
                        LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1
                        WHERE (doorName LIKE '%Build1 - HR Attendance%' 
                        OR doorName LIKE '%Comsumables room%')
                        AND doorID NOT IN ('0000301', '0000401', '0000501', '0000601', '0000701', '0000801', '0000901','0001501')"
                    );

                    foreach (var door in basicDoors)
                    {
                        // ตรวจสอบและเพิ่มสิทธิ์สำหรับบัตร (-1)
                        await AddDoorAccess(
                            personId + "-1",
                            door.doorID,
                            door.weekTimeID,
                            0,
                            connection
                        );

                        // ตรวจสอบว่ามีลายนิ้วมืออยู่หรือไม่
                        var hasFingerprintData = await connection.QueryFirstOrDefaultAsync<int>(
                            @"SELECT COUNT(1) FROM Person WHERE personID = @personId",
                            new { personId = personId + "-2" }
                        );

                        // ถ้ามีลายนิ้วมือ ให้เพิ่มสิทธิ์สำหรับลายนิ้วมือ (-2)
                        if (hasFingerprintData > 0)
                        {
                            await AddDoorAccess(
                                personId + "-2",
                                door.doorID,
                                door.weekTimeID,
                                1,
                                connection
                            );
                        }
                    }

                    return new AccessResult
                    {
                        Success = true,
                        Message = "เพิ่มสิทธิ์ประตูพื้นฐานสำเร็จ",
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding HR access for employee {personId}");
                return new AccessResult { Success = false, Message = ex.Message };
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckAndAddBasicAccess()
        {
            try
            {
                int totalEmployees = 0;
                int updatedEmployees = 0;
                List<string> failedEmployees = new List<string>();

                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูลประตูพื้นฐานที่พนักงานทุกคนควรมี
                    var basicDoors = await db.QueryAsync<dynamic>(
                        @"
                        SELECT doorID, weekTimeID 
                        FROM PubDoor 
                        LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1
                        WHERE doorName LIKE '%Build1 - HR Attendance%' 
                        OR doorName LIKE '%Comsumables room%'"
                    );

                    // ดึงข้อมูลพนักงานทั้งหมดจาก Person ที่มีสิทธิ์บัตร (-1)
                    var employees = await db.QueryAsync<dynamic>(
                        @"
                        SELECT DISTINCT SUBSTRING(personID, 1, LEN(personID) - 2) AS basePersonID
                        FROM Person
                        WHERE personID LIKE '%-1'"
                    );

                    totalEmployees = employees.Count();

                    // วนลูปตรวจสอบและเพิ่มสิทธิ์พื้นฐานให้พนักงานแต่ละคน
                    foreach (var employee in employees)
                    {
                        string basePersonId = employee.basePersonID;
                        bool success = true;

                        try
                        {
                            foreach (var door in basicDoors)
                            {
                                // ตรวจสอบและเพิ่มสิทธิ์สำหรับบัตร (-1)
                                await AddDoorAccess(
                                    basePersonId + "-1",
                                    door.doorID,
                                    door.weekTimeID,
                                    0,
                                    db
                                );

                                // ตรวจสอบว่ามีลายนิ้วมืออยู่หรือไม่
                                var hasFingerprintData = await db.QueryFirstOrDefaultAsync<int>(
                                    @"SELECT COUNT(1) FROM Person WHERE personID = @personId",
                                    new { personId = basePersonId + "-2" }
                                );

                                // ถ้ามีลายนิ้วมือ ให้เพิ่มสิทธิ์สำหรับลายนิ้วมือ (-2)
                                if (hasFingerprintData > 0)
                                {
                                    await AddDoorAccess(
                                        basePersonId + "-2",
                                        door.doorID,
                                        door.weekTimeID,
                                        1,
                                        db
                                    );
                                }
                            }
                            updatedEmployees++;
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            failedEmployees.Add(basePersonId);
                            Console.WriteLine(
                                $"Error adding access for {basePersonId}: {ex.Message}"
                            );
                        }
                    }

                    return Json(
                        new
                        {
                            status = "success",
                            totalEmployees = totalEmployees,
                            updatedEmployees = updatedEmployees,
                            failedEmployees = failedEmployees,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAllAccess(string personId)
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
                {
                    // แทนที่จะลบ ให้ update reserve1 = 2 แทน
                    string updateSql =
                        @"
                        UPDATE PubDoorAuth 
                        SET reserve1 = 2,
                            modifyTime = @modifyTime
                        WHERE personID IN (@personId1, @personId2)";

                    await db.ExecuteAsync(
                        updateSql,
                        new
                        {
                            modifyTime = DateTime.Now,
                            personId1 = personId + "-1",
                            personId2 = personId + "-2",
                        }
                    );

                    return Json(new { status = "success" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        private async Task AddDoorAccess(
            string personId,
            string doorId,
            string weekTimeId,
            int dnfp,
            IDbConnection db
        )
        {
            // ตรวจสอบว่ามีสิทธิ์อยู่แล้วหรือไม่
            var exists = await db.QueryFirstOrDefaultAsync<int>(
                @"
                SELECT COUNT(1) FROM PubDoorAuth 
                WHERE personID = @personId AND doorID = @doorId",
                new { personId, doorId }
            );

            if (exists == 0)
            {
                var parameters = new
                {
                    personID = personId,
                    doorID = doorId,
                    weekTimeID = weekTimeId,
                    holidayGroupID = (string)null,
                    tag = "HTA860PMF",
                    reserve1 = 0,
                    reserve2 = (string)null,
                    reserve3 = (string)null,
                    reserve4 = (string)null,
                    @operator = "SUPERVISOR",
                    UserGroupID = (string)null,
                    modifyTime = DateTime.Now,
                    DoubleCheck = 0,
                    BlackList = 0,
                    Check_TimeZone = 1,
                    Check_Holiday = 1,
                    DownLoad_Fg = dnfp,
                    contolL1_N = (string)null,
                    Master_Card = 0,
                };

                await db.ExecuteAsync(
                    @"
                    INSERT INTO PubDoorAuth (
                        personID, doorID, weekTimeID, holidayGroupID,
                        tag, reserve1, reserve2, reserve3, reserve4,
                        operator, UserGroupID, modifyTime,
                        DoubleCheck, BlackList, Check_TimeZone,
                        Check_Holiday, DownLoad_Fg, contolL1_N, Master_Card
                    ) VALUES (
                        @personID, @doorID, @weekTimeID, @holidayGroupID,
                        @tag, @reserve1, @reserve2, @reserve3, @reserve4,
                        @operator, @UserGroupID, @modifyTime,
                        @DoubleCheck, @BlackList, @Check_TimeZone,
                        @Check_Holiday, @DownLoad_Fg, @contolL1_N, @Master_Card
                    )",
                    parameters
                );
            }
        }

        public async Task<IActionResult> GetEmployeeHistory(string personId)
        {
            try
            {
                // เตรียมข้อมูลพื้นฐาน
                var personIdBase = personId.Split('-')[0];
                var personIdForCard = personIdBase + "-1";
                var cardNumber = "";
                var resultList = new List<dynamic>();

                // 1) ดึงเลขบัตรจาก SQL944
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    await sql944Connection.OpenAsync();
                    var person = await sql944Connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT TOP 1 CAST(cardNumber AS NVARCHAR(50)) AS cardNumber FROM Person WHERE PersonID = @PersonID",
                        new { PersonID = personIdForCard }
                    );
                    if (person == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }
                    cardNumber = person.cardNumber;
                }

                // 2) ดึงประวัติการเปลี่ยนกะจาก vEmpMovement และ map เป็นฟิลด์ที่ child row ต้องใช้
                using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                {
                    await ces941Connection.OpenAsync();

                    var movementHistory = await ces941Connection.QueryAsync<dynamic>(
                        @"SELECT 
                              EmpNo, PrefixName, EmpName, EmpLName, EmpStartDate, EmpResignDate, ActionType, ActionReason, PrimaryKey, EffDateFrom, EffDateTo, EffAttProcess, BIWRequire, ActionTypeDesc, TDepCode, TSecCode, 
                         Gender, BirthDate, TCosCenter, TWorkArea, WrkPlanID, TBossId, BrwType, CreateDate, CreateBy, FlagDel, DepCode, SecCode, CostCenter, WorkArea, BossId, ComNo, TComNo, Language, EmpGroup, ReasonCode, ChangeDate,
                          ChangeBy, ReasonDesc 
                          FROM vEmpMovement
                          WHERE EmpNo = @EmpNo AND ReasonDesc = 'Change shift'
                          ORDER BY EffDateFrom DESC",
                        new { EmpNo = personIdBase }
                    );

                    foreach (var item in movementHistory)
                    {
                        DateTime? effFrom = (DateTime?)item.EffDateFrom;
                        string wrkPlanId = item.WrkPlanID?.ToString();

                        string shiftCode = item.WrkPlanID?.ToString();

                        string fullName = ($"{(string?)item.PrefixName} {(string?)item.EmpName} {(string?)item.EmpLName}").Trim();

                        resultList.Add(new
                        {
                            id = 0,
                            date = effFrom,
                            shiftMent = item.WrkPlanID?.ToString(),
                            personId = personIdBase,
                            name = fullName,
                            deptID = (string)null,
                            deptName = (string)null,
                            deptCode = (string)null,
                            cardNumber = cardNumber,
                            changeReasonCode = (string?)item.ReasonCode,
                            changeReasonDesc = (string?)item.ReasonDesc,
                            wrkPlanId = item.WrkPlanID?.ToString(),
                            actionType = item.ActionType?.ToString(),
                            actionReason = item.ActionReason?.ToString(),
                            primaryKey = item.PrimaryKey?.ToString(),
                            effDateFrom = item.EffDateFrom?.ToString(),
                            effDateTo = item.EffDateTo?.ToString(),
                            effAttProcess = item.EffAttProcess?.ToString(),
                            WorkArea = item.WorkArea?.ToString(),
                            TDepCode = item.TDepCode?.ToString(),
                            TSecCode = item.TSecCode?.ToString(),
                            Gender = item.Gender?.ToString(),
                            BirthDate = item.BirthDate?.ToString(),
                            TCosCenter = item.TCosCenter?.ToString(),
                            TWorkArea = item.TWorkArea?.ToString(),
                            source = "CES"
                        });
                    }

                }

                // 3) ดึงประวัติการเปลี่ยนกะจากตาราง emp_person_shift ในฐานข้อมูลหลัก และรวมผล
                var localHistory = await _context.emp_person_shift
                    .Where(h => (h.personID == personId || h.personID == personIdBase) && h.date != null)
                    .OrderByDescending(h => h.date)
                    .Select(h => new { h.date, h.shiftMent })
                    .ToListAsync();

                foreach (var h in localHistory)
                {
                    resultList.Add(new
                    {
                        id = 0,
                        date = (DateTime?)h.date,
                        shiftMent = h.shiftMent,
                        personId = personIdBase,
                        name = (string)null,
                        deptID = (string)null,
                        deptName = (string)null,
                        deptCode = (string)null,
                        cardNumber = cardNumber,
                        changeReasonCode = (string)null,
                        changeReasonDesc = (string)null,
                        wrkPlanId = (string)null,
                        actionType = (string)null,
                        actionReason = (string)null,
                        primaryKey = (string)null,
                        effDateFrom = h.date != null ? ((DateTime)h.date).ToString() : null,
                        effDateTo = (string)null,
                        effAttProcess = (string)null,
                        WorkArea = (string)null,
                        TDepCode = (string)null,
                        TSecCode = (string)null,
                        Gender = (string)null,
                        BirthDate = (string)null,
                        TCosCenter = (string)null,
                        TWorkArea = (string)null,
                        source = "LOCAL"
                    });
                }

                resultList = resultList
                    .OrderByDescending(x => (DateTime?)x.date)
                    .ToList();

                return Json(new { success = true, data = resultList });


            }
            catch (Exception ex)
            {
                // บันทึกข้อผิดพลาด
                _logger.LogError(ex, "Error in GetEmployeeHistory: {Message}", ex.Message);
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckLatestShiftCompare(string personId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personId))
                {
                    return Json(new { success = false, message = "personId ว่าง" });
                }

                var personIdBase = personId.Split('-')[0];
                var today = DateTime.Today;

                // 1) กะปัจจุบันใน HRM_IPS
                string localShift = null;
                var latestLocal = await _context
                    .emp_person_shift
                    .Where(h => h.personID == personId && h.isActive == 1 && h.date != null)
                    .OrderByDescending(h => h.date)
                    .Select(h => new { h.shiftMent, h.date })
                    .FirstOrDefaultAsync();

                if (latestLocal != null)
                {
                    localShift = latestLocal.shiftMent;
                }
                else
                {
                    var emp = await _context.emp_person
                        .Where(e => e.personID == personId)
                        .Select(e => new { e.shiftMent })
                        .FirstOrDefaultAsync();
                    localShift = emp?.shiftMent;
                }

                // 2) กะล่าสุดฝั่ง HR_IPS (effective วันนี้)
                string hrShift = null;
                using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                {
                    await ces941Connection.OpenAsync();

                    // หา WrkPlanID ล่าสุดที่มีผลวันนี้จาก vEmpMovement
                    var empMovement = await ces941Connection.QueryFirstOrDefaultAsync(
                        @"SELECT TOP 1 
                              CAST(WrkPlanID AS NVARCHAR(50)) AS WrkPlanID
                          FROM vEmpMovement
                          WHERE EmpNo = @EmpNo
                            AND EffAttProcess = 'y'
                            AND @WorkDate BETWEEN EffDateFrom AND ISNULL(EffDateTo, '9999-12-31')
                          ORDER BY EffDateFrom DESC",
                        new { EmpNo = personIdBase, WorkDate = today }
                    );

                    string wrkPlanId = empMovement?.WrkPlanID?.ToString();
                    if (string.IsNullOrEmpty(wrkPlanId))
                    {
                        // ถ้าไม่พบใน vEmpMovement ให้ใช้ vEmployee
                        var vEmp = await ces941Connection.QueryFirstOrDefaultAsync(
                            @"SELECT TOP 1 CAST(WrkPlanID AS NVARCHAR(50)) AS WrkPlanID FROM vEmployee WHERE EmpNo = @EmpNo",
                            new { EmpNo = personIdBase }
                        );
                        wrkPlanId = vEmp?.WrkPlanID?.ToString();
                    }

                    if (!string.IsNullOrEmpty(wrkPlanId))
                    {
                        var workPlan = await ces941Connection.QueryFirstOrDefaultAsync(
                            @"SELECT TOP 1 CAST(ShiftCode AS NVARCHAR(50)) AS ShiftCode
                              FROM MWorkPlan
                              WHERE WrkPlanID = @WrkPlanID AND CAST(WrkDate AS date) = @WorkDate
                              ORDER BY WrkDate DESC",
                            new { WrkPlanID = wrkPlanId, WorkDate = today }
                        );
                        hrShift = workPlan?.ShiftCode?.ToString();
                    }
                }

                var shouldChange = !string.IsNullOrEmpty(localShift)
                    && !string.IsNullOrEmpty(hrShift)
                    && !string.Equals(localShift, hrShift, StringComparison.OrdinalIgnoreCase);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        personId,
                        localShift,
                        hrShift,
                        shouldChange
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckLatestShiftCompare: {Message}", ex.Message);
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        // บันทึกประวัติการเปลี่ยนกะ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShiftHistory(
            [FromBody] SaveShiftHistoryRequest request
        )
        {
            try
            {
                // ตรวจสอบว่ามีข้อมูลที่จำเป็นหรือไม่
                if (
                    string.IsNullOrEmpty(request.PersonId)
                    || string.IsNullOrEmpty(request.ShiftMent)
                    || !request.Date.HasValue
                )
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // ตรวจสอบว่าเป็นการอัปเดตหรือการสร้างใหม่
                if (request.Id > 0)
                {
                    // ค้นหาข้อมูลเดิมโดยใช้ DbCommand
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM emp_person_shift WHERE Id = @Id";
                    command.Parameters.Add(
                        new Microsoft.Data.SqlClient.SqlParameter("@Id", request.Id)
                    );

                    emp_person_shift existingShift = null;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            existingShift = new emp_person_shift
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                personID = reader["personID"]?.ToString() ?? string.Empty,
                                name = reader["name"]?.ToString() ?? string.Empty,
                                deptID = reader["deptID"]?.ToString() ?? string.Empty,
                                deptName = reader["deptName"]?.ToString() ?? string.Empty,
                                deptCode = reader["deptCode"]?.ToString() ?? string.Empty,
                                shiftMent = reader["shiftMent"]?.ToString() ?? string.Empty,
                                date =
                                    reader["date"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["date"])
                                        : DateTime.Now,
                                isActive =
                                    reader["isActive"] != DBNull.Value
                                        ? Convert.ToInt32(reader["isActive"])
                                        : 1,
                            };
                        }
                    }

                    if (existingShift == null)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลกะที่ต้องการแก้ไข" }
                        );
                    }

                    // อัพเดทข้อมูล
                    existingShift.personID = request.PersonId;
                    existingShift.name = FormatEmployeeName(request.name); // ใช้ชื่อที่ตัดแล้ว
                    existingShift.deptID = request.deptID;
                    existingShift.deptName = request.deptName;
                    existingShift.deptCode = request.deptCode;
                    existingShift.shiftMent = request.ShiftMent;
                    existingShift.date = request.Date ?? DateTime.Now;
                    existingShift.isActive = 1;

                    _context.emp_person_shift.Update(existingShift);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "อัปเดตข้อมูลกะเรียบร้อยแล้ว" });
                }
                else
                {
                    // ถ้าไม่มีชื่อพนักงานและข้อมูลแผนก ให้ดึงข้อมูลพนักงานจากฐานข้อมูล
                    if (
                        string.IsNullOrEmpty(request.name)
                        || string.IsNullOrEmpty(request.deptID)
                        || string.IsNullOrEmpty(request.deptName)
                    )
                    {
                        // กำหนดค่าข้อมูลที่จำเป็น
                        request.name = request.name;
                        request.deptID = request.deptID;
                        request.deptName = request.deptName;
                        request.deptCode = request.deptCode;
                    }

                    // สร้างข้อมูลใหม่
                    var newShift = new emp_person_shift
                    {
                        personID = request.PersonId,
                        name = FormatEmployeeName(request.name), // ใช้ชื่อที่ตัดแล้ว
                        deptID = request.deptID,
                        deptName = request.deptName,
                        deptCode = request.deptCode ?? request.deptID,
                        shiftMent = request.ShiftMent,
                        date = request.Date.Value,
                        isActive = 1,
                    };

                    await _context.emp_person_shift.AddAsync(newShift);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "บันทึกข้อมูลกะเรียบร้อยแล้ว" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveShiftHistory: {Message}", ex.Message);
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล: " + ex.Message,
                    }
                );
            }
        }

        // เพิ่มคลาสสำหรับรับข้อมูล
        public class SaveShiftHistoryRequest
        {
            public int Id { get; set; }
            public string PersonId { get; set; }
            public string name { get; set; }
            public string deptID { get; set; }
            public string deptName { get; set; }
            public string deptCode { get; set; }
            public string ShiftMent { get; set; }
            public DateTime? Date { get; set; }
        }

        // ลบประวัติการเปลี่ยนกะ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShiftHistory(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่ามีข้อมูลกะที่ต้องการลบหรือไม่
                    var shift = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT * FROM emp_person_shift WHERE Id = @Id",
                        new { Id = id }
                    );

                    if (shift == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลกะที่ต้องการลบ" });
                    }

                    // ลบข้อมูลออกจากฐานข้อมูล
                    var result = await connection.ExecuteAsync(
                        "DELETE FROM emp_person_shift WHERE Id = @Id",
                        new { Id = id }
                    );

                    if (result > 0)
                    {
                        return Json(new { success = true, message = "ลบข้อมูลกะเรียบร้อยแล้ว" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "ไม่สามารถลบข้อมูลกะได้" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteShiftHistory: {Message}", ex.Message);
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการลบข้อมูล: " + ex.Message }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddShift(
            string personId,
            string date,
            string shiftMent,
            string deptName,
            string deptId,
            string personName
        )
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // อรวจสอบว่ามี shift ในวันที่เลือกแล้วหรือไม่
                    var checkSql =
                        @"
                        SELECT COUNT(1) 
                        FROM emp_person_shift 
                        WHERE personID = @personID 
                        AND date = @date";

                    var exists = await connection.QueryFirstOrDefaultAsync<int>(
                        checkSql,
                        new { personID = personId, date = date }
                    );

                    if (exists > 0)
                    {
                        return Json(
                            new { success = false, message = "มี Shift ในวันที่เลือกแล้ว" }
                        );
                    }

                    // เพิ่ม shift ใหม่
                    var insertSql =
                        @"
                        INSERT INTO emp_person_shift (
                            personID, date, shiftMent, 
                            deptID, deptName, deptCode,
                            name, isActive
                        )
                        VALUES (
                            @personID, @date, @shiftMent,
                            @deptId, @deptName, @deptId, 
                            @personName, @isActive
                        )";

                    await connection.ExecuteAsync(
                        insertSql,
                        new
                        {
                            personID = personId.Split('-')[0],
                            date = date,
                            shiftMent = shiftMent,
                            deptId = deptId,
                            deptName = deptName,
                            personName = personName,
                            isActive = 1,
                        }
                    );

                    return Json(new { success = true, message = "เพิ่ม Shift สำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShift()
        {
            try
            {
                if (string.IsNullOrEmpty(_hrIpsConnectionString))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "ไม่พบการตั้งค่าการเชื่อมต่อกับฐานข้อมูล HR_IPS",
                        }
                    );
                }

                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                using (var mainConnection = new SqlConnection(_connectionString))
                {
                    try
                    {
                        await hrConnection.OpenAsync();
                        await mainConnection.OpenAsync();
                    }
                    catch (Exception ex)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = $"ไม่สามารถเชื่อมต่อกับฐานข้อมูลได้: {ex.Message}",
                            }
                        );
                    }

                    // ดึงข้อมูลจาก hr_ips
                    var selectSql =
                        @"
                        SELECT personID, shiftMent, name, deptID, deptName, cardNumber 
                        FROM emp_person 
                        WHERE personID IS NOT NULL 
                        AND personID != ''
                        AND cardNumber IS NOT NULL";
                    var hrData = await hrConnection.QueryAsync<dynamic>(selectSql);

                    if (!hrData.Any())
                    {
                        return Json(new { success = false, message = "ไม่สบข้อมูลพากระบบ HR_IPS" });
                    }

                    // ดึงข้อมูล personID ที่มีอยู่ในฐานข้อมูลหลัก
                    var existingSql =
                        @"SELECT personID, cardNumber FROM emp_person WHERE personID IS NOT NULL";
                    var existingData = await mainConnection.QueryAsync<dynamic>(existingSql);
                    var existingDict = existingData.ToDictionary(
                        x => x.personID?.ToString(),
                        x => x.cardNumber?.ToString()
                    );

                    int updatedCount = 0;
                    int insertedCount = 0;

                    foreach (var item in hrData)
                    {
                        var personId = item?.personID?.ToString();
                        if (string.IsNullOrEmpty(personId))
                            continue;

                        if (existingDict.ContainsKey(personId))
                        {
                            // อัพเดทข้อมูลที่มีอยู่
                            var updateSql =
                                @"
                                UPDATE emp_person 
                                SET shiftMent = @shiftMent,
                                    cardNumber = @cardNumber,
                                    name = @name,
                                    deptID = @deptID,
                                    deptName = @deptName
                                WHERE personID = @personID";

                            await mainConnection.ExecuteAsync(
                                updateSql,
                                new
                                {
                                    personID = personId,
                                    shiftMent = item.shiftMent,
                                    cardNumber = item.cardNumber,
                                    name = item.name,
                                    deptID = item.deptID,
                                    deptName = item.deptName,
                                }
                            );
                            updatedCount++;
                        }
                        else
                        {
                            // เพิ่มข้อมูลใหม่
                            var insertSql =
                                @"
                                INSERT INTO emp_person (
                                    personID, cardNumber, name, 
                                    deptID, deptName, shiftMent,
                                    createDate, isActive, RecordStatus,
                                    userLevel, status
                                ) VALUES 
                                (@personID, @cardNumber, @name, @deptID, @deptName, @shiftMent, 
                                 @createDate, 1, 'N', '1', '1')";

                            await mainConnection.ExecuteAsync(
                                insertSql,
                                new
                                {
                                    personID = personId,
                                    cardNumber = item.cardNumber,
                                    name = item.name,
                                    deptID = item.deptID,
                                    deptName = item.deptName,
                                    shiftMent = item.shiftMent,
                                    createDate = DateTime.Now,
                                }
                            );
                            insertedCount++;
                        }
                    }

                    return Json(
                        new
                        {
                            success = true,
                            message = $"อัพเดทกะสำเร็จ (อัพเดท {updatedCount} รายการ, เพิ่มใหม่ {insertedCount} รายการ)",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        public async Task<IActionResult> TypeShift()
        {
            try
            {
                // ดึงข้อมูลกะทั้งหมด
                var shifts = await _context
                    .emp_shift.Where(s => s.record_status == "N")
                    .OrderBy(s => s.Id)
                    .Select(s => new emp_shift
                    {
                        Id = s.Id,
                        shift_code = s.shift_code,
                        shift_name = s.shift_name,
                        shift_group = s.shift_group,
                        start_time = s.start_time,
                        end_time = s.end_time,
                        sort = s.sort,
                    })
                    .ToListAsync();

                ViewBag.Shifts = shifts;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TypeShift action: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var shifts = await _context
                    .emp_shift.Where(s => s.record_status == "N")
                    .OrderBy(s => s.sort)
                    .Select(s => new
                    {
                        s.Id,
                        s.shift_code,
                        s.shift_name,
                        s.shift_group,
                        s.start_time,
                        start_duration = s.start_time, // Assuming start_duration is the same as start_time
                        break_start_time = (string)null, // เปลี่ยนเป็น string
                        break_end_time = (string)null, // เปลี่ยนเป็น string
                        break_duration = (TimeSpan?)null,
                        s.end_time,
                        end_duration = s.end_time, // Assuming end_duration is the same as end_time
                        work_3rd_start_time = (string)null, // เปลี่ยนเป็น string
                        work_3rd_end_time = (string)null, // เปลี่ยนเป็น string
                        work_3rd_duration = (TimeSpan?)null,
                        s.sort,
                        ot_start_time = (string)null, // เปลี่ยนเป็น string
                        ot_end_time = (string)null, // เปลี่ยนเป็น string
                        ot_duration = (TimeSpan?)null,
                    })
                    .ToListAsync();

                return Json(new { data = shifts });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftById(int id)
        {
            try
            {
                var shift = await _context
                    .emp_shift.Select(s => new
                    {
                        s.Id,
                        s.shift_code,
                        s.shift_name,
                        s.shift_group,
                        s.start_time,
                        s.end_time,
                        s.sort,
                        s.break_start_time,
                        s.break_end_time,
                        s.ot_start_time,
                        s.ot_end_time,
                    })
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (shift == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลกะ" });
                }

                return Json(shift);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveShift([FromBody] emp_shift shift)
        {
            try
            {
                if (shift == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลกะ" });
                }

                if (shift.Id == 0)
                {
                    // กรณีเพิ่มใหม่
                    shift.record_status = "N";
                    shift.break_start_time = string.IsNullOrEmpty(shift.break_start_time)
                        ? null
                        : shift.break_start_time;
                    shift.break_end_time = string.IsNullOrEmpty(shift.break_end_time)
                        ? null
                        : shift.break_end_time;
                    shift.ot_start_time = string.IsNullOrEmpty(shift.ot_start_time)
                        ? null
                        : shift.ot_start_time;
                    shift.ot_end_time = string.IsNullOrEmpty(shift.ot_end_time)
                        ? null
                        : shift.ot_end_time;

                    _context.emp_shift.Add(shift);
                }
                else
                {
                    // กรณีแก้ไข
                    var existingShift = await _context.emp_shift.FindAsync(shift.Id);
                    if (existingShift == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลกะ" });
                    }

                    // อัพเดทข้อมูลพื้นฐาน
                    existingShift.shift_code = shift.shift_code;
                    existingShift.shift_name = shift.shift_name;
                    existingShift.shift_group = shift.shift_group;
                    existingShift.start_time = shift.start_time;
                    existingShift.end_time = shift.end_time;
                    existingShift.sort = shift.sort;

                    // อัพเดทข้อมูลเวลาพักและ OT
                    existingShift.break_start_time = string.IsNullOrEmpty(shift.break_start_time)
                        ? null
                        : shift.break_start_time;
                    existingShift.break_end_time = string.IsNullOrEmpty(shift.break_end_time)
                        ? null
                        : shift.break_end_time;
                    existingShift.ot_start_time = string.IsNullOrEmpty(shift.ot_start_time)
                        ? null
                        : shift.ot_start_time;
                    existingShift.ot_end_time = string.IsNullOrEmpty(shift.ot_end_time)
                        ? null
                        : shift.ot_end_time;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                var shift = await _context.emp_shift.FindAsync(id);
                if (shift == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลกะ" });
                }

                shift.record_status = "D";
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCardData(
            string personId,
            string cardNumber,
            string personName,
            string deptID = "",
            string deptName = "",
            string shift = "",
            string date = ""
        )
        {
            try
            {
                // บันทึก log เพื่อตรวจสอบข้อมูลที่ได้รับ
                Console.WriteLine(
                    $"CreateCardData - personId: {personId}, cardNumber: {cardNumber}, personName: {personName}, deptID: {deptID}, deptName: {deptName}"
                );

                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(personName))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // ดึงข้อมูลพนักงานจาก CES941 เพื่อตรวจสอบว่ามีพนักงานอยู่จริง
                var ces941Employees = await GetEmployeesFromCES941(ViewData["CurrentUserDepartment"]?.ToString() ?? "");
                var employee = ces941Employees.FirstOrDefault(e => e.EmpNo?.ToString() == personId);

                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานใน CES941" });
                }

                // สร้างข้อมูลพนักงานสำหรับบันทึกลง SQL944
                var cardPersonId = $"{personId}-1"; // รูปแบบสำหรับบัตร
                var fingerPersonId = $"{personId}-2"; // รูปแบบสำหรับลายนิ้วมือ

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (var mainConnection = new SqlConnection(_connectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่
                    var existingCardPerson = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM Person WHERE PersonID = @PersonID",
                        new { PersonID = cardPersonId }
                    );

                    var existingFingerPerson = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM Person WHERE PersonID = @PersonID",
                        new { PersonID = fingerPersonId }
                    );

                    if (existingCardPerson != null || existingFingerPerson != null)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "มีข้อมูลบัตรหรือลายนิ้วมือของพนักงานนี้อยู่แล้ว",
                            }
                        );
                    }

                    // SQL สำหรับเพิ่มข้อมูล
                    var insertPersonSql =
                        @"
                        INSERT INTO Person (
                            CardNumber, PersonID, Name, DeptID, DeptName,
                            UserLevel, Status, ReserveChar1, CardType, CardTypeDesc,
                            SubSystem, UseCategory, UseStatus, CardCategory, CardStatus,
                            reserve3, modifyTime, leaveJobDate, enableDate, disableDate
                        )
                        VALUES (
                            @CardNumber, @PersonID, @Name, @DeptID, @DeptName,
                            @UserLevel, @Status, @ReserveChar1, @CardType, @CardTypeDesc,
                            @SubSystem, @UseCategory, @UseStatus, @CardCategory, @CardStatus,
                            @reserve3, @modifyTime, @leaveJobDate, @enableDate, @disableDate
                        )";

                    // เพิ่มข้อมูลบัตร
                    await sql944Connection.ExecuteAsync(
                        insertPersonSql,
                        new
                        {
                            CardNumber = cardNumber,
                            PersonID = cardPersonId,
                            Name = personName,
                            DeptID = deptID,
                            DeptName = deptName,
                            UserLevel = "29991231",
                            Status = "0",
                            ReserveChar1 = "19991231",
                            CardType = "0",
                            CardTypeDesc = "Normal card",
                            SubSystem = "11111111111111111111",
                            UseCategory = "Normal card",
                            UseStatus = "2",
                            CardCategory = "0",
                            CardStatus = "129",
                            reserve3 = cardNumber,
                            modifyTime = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                        }
                    );

                    // เพิ่มข้อมูลลายนิ้วมือ
                    await sql944Connection.ExecuteAsync(
                        insertPersonSql,
                        new
                        {
                            CardNumber = personId, // ใช้ personId เป็น cardNumber สำหรับลายนิ้วมือ
                            PersonID = fingerPersonId,
                            Name = personName,
                            DeptID = deptID,
                            DeptName = deptName,
                            UserLevel = "29991231",
                            Status = "0",
                            ReserveChar1 = "19991231",
                            CardType = "0",
                            CardTypeDesc = "Fingerprint",
                            SubSystem = "11111111111111111111",
                            UseCategory = "Fingerprint",
                            UseStatus = "2",
                            CardCategory = "0",
                            CardStatus = "129",
                            reserve3 = personId, // ใช้ personId เป็น reserve3 สำหรับลายนิ้วมือ
                            modifyTime = DateTime.Now,
                            leaveJobDate = new DateTime(1999, 12, 31),
                            enableDate = DateTime.Now,
                            disableDate = new DateTime(1999, 12, 31),
                        }
                    );

                    // ไม่ต้องเพิ่มข้อมูลลงใน emp_person_shift ที่นี่
                    // เพราะ SaveShiftHistory จะจัดการเองแล้ว
                }

                // เพิ่มสิทธิ์ประตูพื้นฐาน
                var hrAccessResult = await AddBasicDoorAccess(personId);
                if (!hrAccessResult.Success)
                {
                    _logger.LogWarning(
                        $"ไม่สามารถเพิ่มสิทธิ์ประตูพื้นฐานให้กับพนักงาน {personId}: {hrAccessResult.Message}"
                    );
                }

                return Json(new { success = true, message = "สร้างข้อมูลบัตรและลายนิ้วมือสำเร็จ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateCardData: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เพิ่มคลาสสำหรับเก็บผลลัพธ์การเพิ่มสิทธิ์ประตู
        private class AccessResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        // ฟังก์ชันจัดรูปแบบชื่อพนักงาน
        private string FormatEmployeeName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return fullName;

            // ลบคำนำหน้าชื่อ
            string name = System.Text.RegularExpressions.Regex.Replace(
                fullName,
                @"^(Mr\.|Mrs\.|Miss|Ms\.|Dr\.|Prof\.)\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // แยกชื่อและนามสกุล
            string[] nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 2)
            {
                // เอาเฉพาะตัวอักษรแรกของนามสกุล
                string lastName = nameParts[nameParts.Length - 1];
                string lastNameInitial = lastName.Substring(0, 1);
                // รวมชื่อและตัวอักษรแรกของนามสกุล
                return nameParts[0] + " " + lastNameInitial + ".";
            }
            return name;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFingerprintData(
            string personId,
            string cardNumber,
            string personName,
            string deptID = "",
            string deptName = ""
        )
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(personName))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // ดึงข้อมูลพนักงานจาก CES941 เพื่อตรวจสอบว่ามีพนักงานอยู่จริง
                var ces941Employees = await GetEmployeesFromCES941(ViewData["CurrentUserDepartment"]?.ToString() ?? "");
                var employee = ces941Employees.FirstOrDefault(e => e.EmpNo?.ToString() == personId);

                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานใน CES941" });
                }

                // สร้างข้อมูลพนักงานสำหรับบันทึกลงตาราง BioDataGetList
                var fingerprintPersonId = $"{personId}-2"; // รูปแบบสำหรับลายนิ้วมือ

                // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var existingPerson = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM BioDataGetList WHERE PersonID = @PersonID",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (existingPerson != null)
                    {
                        //อัพเดท GetBioDate กับ Result เป็น NULL
                        await sql944Connection.ExecuteAsync(
                            "UPDATE BioDataGetList SET GetBioDate = NULL, Result = NULL WHERE PersonID = @PersonID",
                            new { PersonID = fingerprintPersonId }
                        );
                        return Json(
                            new { success = true, message = "อัพเดทข้อมูลลายนิ้วมือสำเร็จ" }
                        );
                    }

                    // บันทึกข้อมูลลงตาราง BioDataGetList
                    var insertSql =
                        @"
                        INSERT INTO BioDataGetList (
                            PersonID, CardNumber, PersonName, ModifyDate, GetBioDate,
                            DeployDeviceID, DeployDevice_IP, IP_Port, DeployDevice_No,
                            DoorName, Result, reserve1, reserve2, productType
                        )
                        VALUES (
                            @PersonID, @CardNumber, @PersonName, @ModifyDate, @GetBioDate,
                            @DeployDeviceID, @DeployDevice_IP, @IP_Port, @DeployDevice_No,
                            @DoorName, @Result, @reserve1, @reserve2, @productType
                        )";

                    await sql944Connection.ExecuteAsync(
                        insertSql,
                        new
                        {
                            PersonID = fingerprintPersonId,
                            CardNumber = personId, // ใช้เลขพนักงานที่ไม่มี -1 หรือ -2
                            PersonName = personName,
                            ModifyDate = DateTime.Now,
                            GetBioDate = (DateTime?)null, // เริ่มต้นเป็น NULL
                            DeployDeviceID = "0000000017",
                            DeployDevice_IP = "10.24.178.12",
                            IP_Port = "4660",
                            DeployDevice_No = 1,
                            DoorName = deptName,
                            Result = (string)null, // เริ่มต้นเป็น NULL
                            reserve1 = (string)null,
                            reserve2 = (string)null,
                            productType = "HTA860PMF",
                        }
                    );
                }

                return Json(new { success = true, message = "สร้างข้อมูลลายนิ้วมือสำเร็จ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateFingerprintData: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับดึงข้อมูลลายนิ้วมือทั้งหมดจากตาราง BioDataGetList
        [HttpGet]
        [Route("Employee/GetAllFingerprintData")]
        public async Task<IActionResult> GetAllFingerprintData()
        {
            try
            {
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var fingerprintData = await sql944Connection.QueryAsync(
                        @"
                        SELECT 
                            PersonID,
                            CardNumber,
                            PersonName,
                            ModifyDate,
                            GetBioDate,
                            DeployDeviceID,
                            DeployDevice_IP,
                            IP_Port,
                            DeployDevice_No,
                            DoorName,
                            Result,
                            reserve1,
                            reserve2,
                            productType
                        FROM BioDataGetList 
                        WHERE PersonID LIKE '%-2'
                        ORDER BY ModifyDate DESC
                    "
                    );

                    return Json(new { success = true, data = fingerprintData });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllFingerprintData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับดึงข้อมูลลายนิ้วมือของพนักงานเฉพาะคน
        [HttpGet]
        [Route("Employee/GetFingerprintDataByPersonId")]
        public async Task<IActionResult> GetFingerprintDataByPersonId(string personId)
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                var fingerprintPersonId = $"{personId}-2";

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var fingerprintData = await sql944Connection.QueryFirstOrDefaultAsync(
                        @"
                        SELECT 
                            PersonID,
                            CardNumber,
                            PersonName,
                            ModifyDate,
                            GetBioDate,
                            DeployDeviceID,
                            DeployDevice_IP,
                            IP_Port,
                            DeployDevice_No,
                            DoorName,
                            Result,
                            reserve1,
                            reserve2,
                            productType
                        FROM BioDataGetList 
                        WHERE PersonID = @PersonID
                    ",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (fingerprintData == null)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลลายนิ้วมือของพนักงานนี้" }
                        );
                    }

                    return Json(new { success = true, data = fingerprintData });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFingerprintDataByPersonId: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับอัพเดทข้อมูลลายนิ้วมือในตาราง BioDataGetList
        [HttpPost]
        [Route("Employee/UpdateFingerprintData")]
        public async Task<IActionResult> UpdateFingerprintData(
            string personId,
            string cardNumber = null,
            string name = null,
            string deptID = null,
            string deptName = null,
            DateTime? getBioDate = null,
            string result = null,
            string status = null
        )
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                var fingerprintPersonId = $"{personId}-2";

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลในตาราง BioDataGetList อยู่หรือไม่
                    var existingData = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM BioDataGetList WHERE PersonID = @PersonID",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (existingData == null)
                    {
                        // ถ้าไม่มีข้อมูล ให้สร้างข้อมูลใหม่ในตาราง BioDataGetList
                        var insertSql =
                            @"
                            INSERT INTO BioDataGetList (
                                PersonID, CardNumber, PersonName, ModifyDate, GetBioDate,
                                DeployDeviceID, DeployDevice_IP, IP_Port, DeployDevice_No,
                                DoorName, Result, reserve1, reserve2, productType
                            )
                            VALUES (
                                @PersonID, @CardNumber, @PersonName, @ModifyDate, @GetBioDate,
                                @DeployDeviceID, @DeployDevice_IP, @IP_Port, @DeployDevice_No,
                                @DoorName, @Result, @reserve1, @reserve2, @productType
                            )";

                        await sql944Connection.ExecuteAsync(
                            insertSql,
                            new
                            {
                                PersonID = fingerprintPersonId,
                                CardNumber = personId, // ใช้เลขพนักงานที่ไม่มี -1 หรือ -2
                                PersonName = name,
                                ModifyDate = DateTime.Now,
                                GetBioDate = getBioDate,
                                DeployDeviceID = "0000000017",
                                DeployDevice_IP = "10.24.178.12",
                                IP_Port = "4660",
                                DeployDevice_No = 1,
                                DoorName = deptName,
                                Result = result,
                                reserve1 = (string)null,
                                reserve2 = (string)null,
                                productType = "HTA860PMF",
                            }
                        );
                    }
                    else
                    {
                        // ถ้ามีข้อมูลอยู่แล้ว ให้อัพเดทข้อมูล
                        var updateData = new DynamicParameters();
                        updateData.Add("@PersonID", fingerprintPersonId);

                        var updateFields = new List<string>();

                        // อัพเดท CardNumber เสมอ (รวมถึงค่าว่าง)
                        updateFields.Add("CardNumber = @CardNumber");
                        updateData.Add("@CardNumber", personId);

                        // อัพเดท PersonName เสมอ (รวมถึงค่าว่าง)
                        updateFields.Add("PersonName = @PersonName");
                        updateData.Add("@PersonName", name);

                        // อัพเดท GetBioDate เสมอ (รวมถึงค่าว่าง)
                        updateFields.Add("GetBioDate = @GetBioDate");
                        updateData.Add("@GetBioDate", getBioDate);

                        // อัพเดท Result เสมอ (รวมถึงค่าว่าง)
                        updateFields.Add("Result = @Result");
                        updateData.Add("@Result", result);

                        // อัพเดท ModifyDate เสมอ
                        updateFields.Add("ModifyDate = @ModifyDate");
                        updateData.Add("@ModifyDate", DateTime.Now);

                        var updateSql =
                            $"UPDATE BioDataGetList SET {string.Join(", ", updateFields)} WHERE PersonID = @PersonID";
                        await sql944Connection.ExecuteAsync(updateSql, updateData);
                    }
                }

                return Json(new { success = true, message = "อัพเดทข้อมูลลายนิ้วมือสำเร็จ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateFingerprintData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับลบข้อมูลลายนิ้วมือ
        [HttpPost]
        [Route("Employee/DeleteFingerprintData")]
        public async Task<IActionResult> DeleteFingerprintData(string personId)
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                var fingerprintPersonId = $"{personId}-2";

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลอยู่หรือไม่
                    var existingData = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM BioDataGetList WHERE PersonID = @PersonID",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (existingData == null)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลลายนิ้วมือของพนักงานนี้" }
                        );
                    }

                    // ลบข้อมูล
                    await sql944Connection.ExecuteAsync(
                        "DELETE FROM BioDataGetList WHERE PersonID = @PersonID",
                        new { PersonID = fingerprintPersonId }
                    );
                }

                return Json(new { success = true, message = "ลบข้อมูลลายนิ้วมือสำเร็จ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteFingerprintData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับตรวจสอบสถานะข้อมูลลายนิ้วมือ
        [HttpGet]
        [Route("Employee/CheckFingerprintStatus")]
        public async Task<IActionResult> CheckFingerprintStatus(string personId)
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "รหัสพนักงานไม่ถูกต้อง" });
                }

                var fingerprintPersonId = $"{personId}-2";

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var fingerprintData = await sql944Connection.QueryFirstOrDefaultAsync(
                        @"
                        SELECT 
                            PersonID,
                            CardNumber,
                            PersonName,
                            GetBioDate,
                            Result,
                            ModifyDate,
                            DoorName
                        FROM BioDataGetList 
                        WHERE PersonID = @PersonID
                    ",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (fingerprintData == null)
                    {
                        return Json(
                            new
                            {
                                success = true,
                                hasData = false,
                                message = "ไม่มีข้อมูลลายนิ้วมือ",
                            }
                        );
                    }

                    var hasBioData = fingerprintData.GetBioDate != null;
                    var hasResult = !string.IsNullOrEmpty(fingerprintData.Result?.ToString());

                    return Json(
                        new
                        {
                            success = true,
                            hasData = true,
                            hasBioData = hasBioData,
                            hasResult = hasResult,
                            data = fingerprintData,
                            message = hasBioData && hasResult
                                ? "มีข้อมูลลายนิ้วมือครบถ้วน"
                                : "มีข้อมูลลายนิ้วมือแต่ยังไม่ครบถ้วน",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckFingerprintStatus: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับดึงข้อมูลลายนิ้วมือที่ยังไม่ครบถ้วน
        [HttpGet]
        [Route("Employee/GetIncompleteFingerprintData")]
        public async Task<IActionResult> GetIncompleteFingerprintData()
        {
            try
            {
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var incompleteData = await sql944Connection.QueryAsync(
                        @"
                        SELECT 
                            PersonID,
                            CardNumber,
                            PersonName,
                            ModifyDate,
                            GetBioDate,
                            Result,
                            DoorName
                        FROM BioDataGetList 
                        WHERE PersonID LIKE '%-2'
                        AND (GetBioDate IS NULL OR Result IS NULL OR Result = '')
                        ORDER BY ModifyDate DESC
                    "
                    );

                    return Json(new { success = true, data = incompleteData });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetIncompleteFingerprintData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับดึงข้อมูลลายนิ้วมือที่ครบถ้วนแล้ว
        [HttpGet]
        [Route("Employee/GetCompleteFingerprintData")]
        public async Task<IActionResult> GetCompleteFingerprintData()
        {
            try
            {
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var completeData = await sql944Connection.QueryAsync(
                        @"
                        SELECT 
                            PersonID,
                            CardNumber,
                            PersonName,
                            ModifyDate,
                            GetBioDate,
                            Result,
                            DoorName
                        FROM BioDataGetList 
                        WHERE PersonID LIKE '%-2'
                        AND GetBioDate IS NOT NULL 
                        AND Result IS NOT NULL 
                        AND Result != ''
                        ORDER BY ModifyDate DESC
                    "
                    );

                    return Json(new { success = true, data = completeData });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCompleteFingerprintData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("Employee/GetDepartmentsList")]
        public IActionResult GetDepartmentsList()
        {
            try
            {
                var departments = GetDepartments();
                return Json(departments);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เพิ่มฟังก์ชัน GetEmployeesList กลับมา
        [HttpPost]
        [Route("Employee/GetEmployeesList")]
        public async Task<IActionResult> GetEmployeesList(
            string department = null,
            string shift = null,
            string status = null,
            string door = null
        )
        {
            try
            {
                // ดึงข้อมูลพนักงานจาก CES941
                var ces941Employees = await GetEmployeesFromCES941(ViewData["CurrentUserDepartment"]?.ToString() ?? "");

                // ดึงข้อมูลสิทธิ์การเข้าประตูจากฐานข้อมูล sql944
                var doorAccess = await GetDoorAccessFromSQL944();

                // ดึงข้อมูลคนที่เก็บข้อมูลลายนิ้วมือแล้ว
                var fingerprintEmployees = await GetFingerprintEmployeesFromSQL944();

                // ตรวจสอบว่ามีข้อมูลหรือไม่
                if (ces941Employees == null)
                {
                    ces941Employees = new List<dynamic>();
                }

                Console.WriteLine($"จำนวนพนักงานจาก CES941: {ces941Employees.Count}");

                // ดึงข้อมูลกะจากฐานข้อมูลหลัก
                var shifts = await _context
                    .emp_person_shift.Where(s => s.personID != null)
                    .GroupBy(s => s.personID)
                    .Select(g => new
                    {
                        PersonID = g.Key,
                        ShiftMent = g.OrderByDescending(s => s.date).FirstOrDefault().shiftMent,
                    })
                    .ToDictionaryAsync(k => k.PersonID, v => v.ShiftMent);

                // ดึงข้อมูลจาก SQL944 เพื่อตรวจสอบสถานะและเลขบัตร
                List<dynamic> cardData = new List<dynamic>();
                List<dynamic> fingerprintData = new List<dynamic>();

                // สร้าง Dictionary เพื่อเก็บจำนวนสิทธิ์การเข้าประตูของแต่ละคน
                var doorAccessCountDict = new Dictionary<string, int>();

                // นับจำนวนสิทธิ์การเข้าประตูของแต่ละคน
                foreach (var access in doorAccess)
                {
                    string personId = access.personID?.ToString();
                    if (string.IsNullOrEmpty(personId))
                        continue;

                    // ตัด -1 หรือ -2 ออกเพื่อให้เป็น base personId
                    string baseId = personId;
                    if (personId.Contains("-"))
                    {
                        baseId = personId.Split('-')[0];
                    }

                    // นับเฉพาะสิทธิ์ที่ยังไม่ถูกยกเลิก (reserve1 != 2)
                    if (access.reserve1 == null || access.reserve1.ToString() != "2")
                    {
                        if (!doorAccessCountDict.ContainsKey(baseId))
                        {
                            doorAccessCountDict[baseId] = 1;
                        }
                        else
                        {
                            doorAccessCountDict[baseId]++;
                        }
                    }
                }

                // สร้าง Dictionary เพื่อเก็บข้อมูลลายนิ้วมือ
                var hasFingerprintDict = new Dictionary<string, bool>();

                // ตรวจสอบว่าพนักงานมีข้อมูลลายนิ้วมือหรือไม่
                foreach (var fp in fingerprintEmployees)
                {
                    string personId = fp.PersonID?.ToString();
                    if (string.IsNullOrEmpty(personId))
                        continue;

                    // ตัด -1 หรือ -2 ออกเพื่อให้เป็น base personId
                    string baseId = personId;
                    if (personId.Contains("-"))
                    {
                        baseId = personId.Split('-')[0];
                    }

                    // ตรวจสอบว่ามีข้อมูลลายนิ้วมือหรือไม่ (FP1 หรือ FP2 ไม่เป็น null)
                    bool hasFP = fp.FP1 != null || fp.FP2 != null;

                    if (hasFP)
                    {
                        hasFingerprintDict[baseId] = true;
                    }
                }

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    try
                    {
                        // ดึงข้อมูลบัตร (PersonID ลงท้ายด้วย -1)
                        cardData = (
                            await sql944Connection.QueryAsync(
                                "SELECT PersonID, CardNumber, Status, DeptID, DeptName, Name FROM Person WHERE PersonID LIKE '%-1'"
                            )
                        ).ToList();

                        Console.WriteLine($"จำนวนข้อมูลบัตรจาก SQL944: {cardData.Count}");

                        // ดึงข้อมูลลายนิ้วมือ (PersonID ลงท้ายด้วย -2)
                        fingerprintData = (
                            await sql944Connection.QueryAsync(
                                "SELECT PersonID, CardNumber, Status, DeptID, DeptName, Name FROM Person WHERE PersonID LIKE '%-2'"
                            )
                        ).ToList();

                        Console.WriteLine(
                            $"จำนวนข้อมูลลายนิ้วมือจาก SQL944: {fingerprintData.Count}"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching SQL944 data: {ex.Message}");
                    }
                }

                // สร้าง Dictionary เพื่อเก็บข้อมูลจาก SQL944 แยกตามประเภท (บัตร/ลายนิ้วมือ)
                var sql944CardDict = new Dictionary<string, dynamic>();
                var sql944FingerprintDict = new Dictionary<string, dynamic>();
                var nameDict = new Dictionary<string, string>();
                var cardNumberDict = new Dictionary<string, string>(); // Dictionary เก็บเฉพาะ cardNumber จาก personId ที่มี -1
                var sql944OnlyPersonIds = new HashSet<string>(); // เก็บ personId ที่มีแค่ใน SQL944

                // ประมวลผลข้อมูลบัตร
                foreach (var person in cardData)
                {
                    string personId = person.PersonID?.ToString();
                    if (string.IsNullOrEmpty(personId) || !personId.EndsWith("-1"))
                        continue;

                    string baseId = personId.Substring(0, personId.Length - 2);
                    string name = person.Name?.ToString();
                    string cardNumber = person.CardNumber?.ToString();

                    sql944CardDict[baseId] = person;
                    sql944OnlyPersonIds.Add(baseId); // เพิ่มลงใน HashSet เพื่อตรวจสอบกับ CES941 ภายหลัง

                    if (!string.IsNullOrEmpty(name))
                    {
                        nameDict[baseId] = name;
                    }

                    if (!string.IsNullOrEmpty(cardNumber))
                    {
                        cardNumberDict[baseId] = cardNumber;
                        Console.WriteLine(
                            $"เพิ่ม cardNumber: {cardNumber} สำหรับ personId: {baseId}"
                        );
                    }
                }

                // ประมวลผลข้อมูลลายนิ้วมือ
                foreach (var person in fingerprintData)
                {
                    string personId = person.PersonID?.ToString();
                    if (string.IsNullOrEmpty(personId) || !personId.EndsWith("-2"))
                        continue;

                    string baseId = personId.Substring(0, personId.Length - 2);
                    string name = person.Name?.ToString();

                    sql944FingerprintDict[baseId] = person;
                    sql944OnlyPersonIds.Add(baseId); // เพิ่มลงใน HashSet เพื่อตรวจสอบกับ CES941 ภายหลัง

                    if (!string.IsNullOrEmpty(name) && !nameDict.ContainsKey(baseId))
                    {
                        nameDict[baseId] = name;
                    }
                }

                // สร้างรายการพนักงานที่รวมข้อมูลจากทั้งสามแหล่ง
                var employees = new List<object>();

                // ตรวจสอบและลบ personId ที่มีใน CES941 ออกจาก sql944OnlyPersonIds
                foreach (var emp in ces941Employees)
                {
                    string empNo = emp.EmpNo?.ToString();
                    if (string.IsNullOrEmpty(empNo))
                    {
                        continue;
                    }

                    // ลบ personId ที่มีใน CES941 ออกจากรายการ personId ที่มีแค่ใน SQL944
                    sql944OnlyPersonIds.Remove(empNo);

                    // ตรวจสอบว่ามีข้อมูลใน SQL944 หรือไม่
                    bool hasCardData = sql944CardDict.ContainsKey(empNo);
                    bool hasFingerprintData = sql944FingerprintDict.ContainsKey(empNo);

                    // ดึงข้อมูลจาก SQL944 ถ้ามี
                    dynamic cardInfo = null;
                    dynamic fingerprintInfo = null;

                    if (hasCardData)
                        sql944CardDict.TryGetValue(empNo, out cardInfo);
                    if (hasFingerprintData)
                        sql944FingerprintDict.TryGetValue(empNo, out fingerprintInfo);

                    // ใช้ข้อมูลจากบัตรเป็นหลัก ถ้าไม่มีให้ใช้ข้อมูลจากลายนิ้วมือ
                    dynamic sql944Info = cardInfo ?? fingerprintInfo;

                    // ดึงข้อมูลกะ
                    string shiftMent = null;
                    shifts.TryGetValue(empNo, out shiftMent);

                    // สร้างชื่อพนักงานจาก CES941 โดยตรวจสอบค่า null
                    string ces941Name = "";
                    if (!string.IsNullOrEmpty(emp.PrefixName?.ToString()))
                        ces941Name += emp.PrefixName.ToString() + " ";
                    if (!string.IsNullOrEmpty(emp.EmpName?.ToString()))
                        ces941Name += emp.EmpName.ToString() + " ";
                    if (!string.IsNullOrEmpty(emp.EmpLName?.ToString()))
                        ces941Name += emp.EmpLName.ToString();
                    ces941Name = ces941Name.Trim(); 

                    // ใช้ชื่อจาก SQL944 ถ้ามี มิฉะนั้นใช้ชื่อจาก CES941
                    string employeeName =
                        nameDict.ContainsKey(empNo) && !string.IsNullOrEmpty(nameDict[empNo])
                            ? nameDict[empNo]
                            : (string.IsNullOrEmpty(ces941Name) ? "test1" : ces941Name);

                    // ใช้ cardNumber จาก personId ที่มี -1 เท่านั้น
                    string employeeCardNumber = "ไม่ระบุ";
                    if (
                        cardNumberDict.ContainsKey(empNo)
                        && !string.IsNullOrEmpty(cardNumberDict[empNo])
                    )
                    {
                        employeeCardNumber = cardNumberDict[empNo];
                    }
                    else if (!string.IsNullOrEmpty(emp.PersonalID?.ToString()))
                    {
                        // ถ้าไม่มีใน SQL944 ให้ใช้ PersonalID จาก CES941 เป็นสำรอง
                        employeeCardNumber = emp.PersonalID.ToString();
                    }

                    // ดึงจำนวนสิทธิ์การเข้าประตู
                    int doorAccessCount = 0;
                    doorAccessCountDict.TryGetValue(empNo, out doorAccessCount);

                    // ตรวจสอบว่ามีข้อมูลลายนิ้วมือหรือไม่
                    bool hasFingerprint =
                        hasFingerprintDict.ContainsKey(empNo) && hasFingerprintDict[empNo];

                    // สร้างข้อมูลพนักงาน
                    var employee = new
                    {
                        id = empNo ?? "",
                        cardNumber = employeeCardNumber,
                        personID = empNo ?? "",
                        name = employeeName,
                        deptID = (sql944Info?.DeptID ?? "").ToString(),
                        deptName = (sql944Info?.DeptName ?? "").ToString(),
                        status = (sql944Info?.Status ?? "0").ToString(),
                        isActive = hasCardData || hasFingerprintData ? 1 : 0,
                        shiftMent = shiftMent ?? "",
                        // เพิ่มข้อมูลอื่นๆ ที่ต้องการจาก CES941
                        empStartDate = emp.EmpStartDate?.ToString("yyyy-MM-dd") ?? "",
                        empResignDate = emp.EmpResignDate?.ToString("yyyy-MM-dd") ?? "",
                        personalID = emp.PersonalID?.ToString() ?? "",
                        gender = emp.Gender?.ToString() ?? "",
                        mobilePhone = emp.MobilePhone?.ToString() ?? "",
                        email = emp.eMailAdd?.ToString() ?? "",
                        // เพิ่มข้อมูลสถานะการมีข้อมูลใน SQL944
                        hasCardData = hasCardData,
                        hasFingerprintData = hasFingerprintData,
                        // เพิ่มข้อมูลจำนวนสิทธิ์การเข้าประตู
                        doorAccessCount = doorAccessCount,
                        // เพิ่มข้อมูลสถานะการมีลายนิ้วมือ
                        hasFingerprint = hasFingerprint,
                        // เพิ่มสถานะว่าเป็นข้อมูลที่สมบูรณ์ (มีทั้งใน CES941 และ SQL944 ถ้าเป็น status 1)
                        isComplete = true,
                    };

                    employees.Add(employee);
                }

                // เพิ่มพนักงานที่มีเฉพาะใน SQL944 เข้าไปด้วย (ไม่มีใน CES941)
                foreach (var personId in sql944OnlyPersonIds)
                {
                    bool hasCardData = sql944CardDict.ContainsKey(personId);
                    bool hasFingerprintData = sql944FingerprintDict.ContainsKey(personId);

                    if (!hasCardData && !hasFingerprintData)
                        continue;

                    // ดึงข้อมูลจาก SQL944
                    dynamic cardInfo = null;
                    dynamic fingerprintInfo = null;

                    if (hasCardData)
                        sql944CardDict.TryGetValue(personId, out cardInfo);
                    if (hasFingerprintData)
                        sql944FingerprintDict.TryGetValue(personId, out fingerprintInfo);

                    // ใช้ข้อมูลจากบัตรเป็นหลัก ถ้าไม่มีให้ใช้ข้อมูลจากลายนิ้วมือ
                    dynamic sql944Info = cardInfo ?? fingerprintInfo;

                    // ดึงข้อมูลกะ
                    string shiftMent = null;
                    shifts.TryGetValue(personId, out shiftMent);

                    // ดึงชื่อจาก SQL944
                    string employeeName = null;
                    if (nameDict.ContainsKey(personId) && !string.IsNullOrEmpty(nameDict[personId]))
                    {
                        employeeName = nameDict[personId];
                    }

                    // ดึง cardNumber
                    string employeeCardNumber = "ไม่ระบุ";
                    if (
                        cardNumberDict.ContainsKey(personId)
                        && !string.IsNullOrEmpty(cardNumberDict[personId])
                    )
                    {
                        employeeCardNumber = cardNumberDict[personId];
                    }

                    // ดึงจำนวนสิทธิ์การเข้าประตู
                    int doorAccessCount = 0;
                    doorAccessCountDict.TryGetValue(personId, out doorAccessCount);

                    // ตรวจสอบว่ามีข้อมูลลายนิ้วมือหรือไม่
                    bool hasFingerprint =
                        hasFingerprintDict.ContainsKey(personId) && hasFingerprintDict[personId];

                    // สร้างข้อมูลพนักงานที่มีแค่ใน SQL944
                    var employee = new
                    {
                        id = personId,
                        cardNumber = employeeCardNumber,
                        personID = personId,
                        name = employeeName,
                        deptID = (sql944Info?.DeptID ?? "").ToString(),
                        deptName = (sql944Info?.DeptName ?? "").ToString(),
                        status = (sql944Info?.Status ?? "0").ToString(),
                        isActive = hasCardData || hasFingerprintData ? 1 : 0,
                        shiftMent = shiftMent ?? "",
                        // ข้อมูลอื่นๆ ที่ไม่มี เนื่องจากไม่มีใน CES941
                        empStartDate = "",
                        empResignDate = "",
                        personalID = "",
                        gender = "",
                        mobilePhone = "",
                        email = "",
                        // ข้อมูลเกี่ยวกับบัตรและลายนิ้วมือ
                        hasCardData = hasCardData,
                        hasFingerprintData = hasFingerprintData,
                        doorAccessCount = doorAccessCount,
                        hasFingerprint = hasFingerprint,
                        // ระบุว่าเป็นข้อมูลที่ไม่สมบูรณ์ (มีเฉพาะใน SQL944)
                        isComplete = false,
                    };

                    // เพิ่มพนักงานเฉพาะที่มีชื่อ
                    if (!string.IsNullOrWhiteSpace(employeeName))
                    {
                        employees.Add(employee);
                    }
                }

                Console.WriteLine($"จำนวนพนักงานหลังการประมวลผล: {employees.Count}");
                Console.WriteLine($"จำนวน cardNumber ที่พบ: {cardNumberDict.Count}");
                Console.WriteLine($"จำนวนพนักงานที่มีแค่ใน SQL944: {sql944OnlyPersonIds.Count}");

                // กรองข้อมูลตามเงื่อนไข
                // กรองตามสถานะการลาออก
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "active")
                    {
                        employees = employees
                            .Where(e => ((dynamic)e).empResignDate == null)
                            .ToList();
                    }
                    else if (status == "resigned")
                    {
                        employees = employees
                            .Where(e => ((dynamic)e).empResignDate != null)
                            .ToList();
                    }
                }

                if (!string.IsNullOrEmpty(department) && department != "all")
                {
                    employees = employees.Where(e => ((dynamic)e).deptID == department).ToList();
                }

                if (!string.IsNullOrEmpty(shift) && shift != "all")
                {
                    employees = employees.Where(e => ((dynamic)e).shiftMent == shift).ToList();
                }

                // กรองตามประตู (ถ้ามีการเลือกประตู)
                if (!string.IsNullOrEmpty(door) && door != "all")
                {
                    try
                    {
                        using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                        {
                            // ดึงรายการ personID ที่มีสิทธิ์เข้าประตูที่เลือก
                            var doorAccessSql =
                                @"
                                SELECT DISTINCT personID 
                                FROM PubDoorAuth 
                                WHERE doorID = @doorID 
                                AND (TRY_CAST(reserve1 AS INT) IS NULL OR TRY_CAST(reserve1 AS INT) != 2)"; // เฉพาะที่ยังไม่ถูกยกเลิกสิทธิ์

                            var authorizedPersons = await sql944Connection.QueryAsync<string>(
                                doorAccessSql,
                                new { doorID = door }
                            );

                            // แปลง personID เป็นรูปแบบที่ไม่มี -1 หรือ -2
                            var basePersonIds = authorizedPersons
                                .Select(p => p.Split('-')[0])
                                .Distinct()
                                .ToHashSet();

                            employees = employees
                                .Where(e => basePersonIds.Contains(((dynamic)e).id.ToString()))
                                .ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error filtering by door: {ex.Message}");
                    }
                }

                Console.WriteLine($"จำนวนพนักงานหลังการกรอง: {employees.Count}");

                return Json(new { success = true, data = employees });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployeesList: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message,
                        stackTrace = ex.StackTrace,
                    }
                );
            }
        }

        private async Task<List<dynamic>> GetDoorAccessFromSQL944()
        {
            using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
            {
                return (
                    await sql944Connection.QueryAsync<dynamic>("SELECT * FROM PubDoorAuth")
                ).ToList();
            }
        }

        private async Task<List<dynamic>> GetFingerprintEmployeesFromSQL944()
        {
            using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
            {
                return (
                    await sql944Connection.QueryAsync<dynamic>(
                        "SELECT CardNumber, FP1, FP2, DeployDeviceName, DeployDevice_Id, PersonName, PersonID, IsDelete, reserve1, reserve2, reserve3, reserve4, modifytime, operator, FV1, FV2, FACE_FEA, FA1, FA2, F51, F52, F90 FROM Person_FP"
                    )
                ).ToList();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddShiftForPerson(
            string personId,
            string shift,
            string date,
            string namePerson
        )
        {
            try
            {
                if (
                    string.IsNullOrEmpty(personId)
                    || string.IsNullOrEmpty(shift)
                    || string.IsNullOrEmpty(date)
                    || string.IsNullOrEmpty(namePerson)
                )
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                using (var connection2 = new SqlConnection(_connectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลพนักงานหรือไม่
                    var employeeSql =
                        @"
                        SELECT TOP 1 name, deptID, deptName 
                        FROM person 
                        WHERE personID LIKE @personIdPattern";

                    var employee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        employeeSql,
                        new { personIdPattern = basePersonId + "%" }
                    );

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // ตรวจสอบว่ามีกะในวันที่เลือกแล้วหรือไม่
                    var checkSql =
                        @"
                        SELECT COUNT(1) 
                        FROM emp_person_shift 
                        WHERE personID = @personID 
                        AND date = @date";

                    var exists = await connection2.QueryFirstOrDefaultAsync<int>(
                        checkSql,
                        new { personID = basePersonId, date = date }
                    );

                    if (exists > 0)
                    {
                        return Json(new { success = false, message = "มีกะในวันที่เลือกแล้ว" });
                    }

                    // เพิ่มกะใหม่
                    var insertSql =
                        @"
                        INSERT INTO emp_person_shift (
                            personID, date, shiftMent, 
                            deptID, deptName, deptCode,
                            name, isActive
                        )
                        VALUES (
                            @personID, @date, @shiftMent,
                            @deptID, @deptName, @deptID, 
                            @name, @isActive
                        )";

                    await connection2.ExecuteAsync(
                        insertSql,
                        new
                        {
                            personID = basePersonId,
                            date = date,
                            shiftMent = shift,
                            deptID = employee.deptID,
                            deptName = employee.deptName,
                            name = namePerson,
                            isActive = 1,
                        }
                    );

                    return Json(new { success = true, message = "เพิ่มกะการทำงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftHistory(string personId)
        {
            try
            {
                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                // ดึงข้อมูลประวัติการเปลี่ยนกะจากตาราง emp_person_shift
                var history = await _context
                    .emp_person_shift.Where(h => h.personID == basePersonId)
                    .OrderBy(h => h.date)
                    .Select(h => new
                    {
                        id = h.Id,
                        date = h.date,
                        shiftMent = h.shiftMent,
                        personID = h.personID,
                        name = h.name,
                        deptID = h.deptID,
                        deptName = h.deptName,
                        deptCode = h.deptCode
                            ?? h.deptID // ใช้ deptID เป็น deptCode ถ้าไม่มี deptCode
                        ,
                    })
                    .ToListAsync();

                return Json(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetShiftHistory: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        // เพิ่ม Method สำหรับดึงข้อมูลกะตาม ID
        [HttpGet]
        public async Task<IActionResult> GetShiftHistoryById(int id)
        {
            try
            {
                // ดึงข้อมูลกะตาม ID
                var shift = await _context
                    .emp_person_shift.Where(h => h.Id == id && h.isActive == 1)
                    .Select(h => new
                    {
                        id = h.Id,
                        date = h.date,
                        shiftMent = h.shiftMent,
                        personID = h.personID,
                        name = h.name,
                        deptID = h.deptID,
                        deptName = h.deptName,
                        deptCode = h.deptCode ?? h.deptID, // ใช้ deptID เป็น deptCode ถ้าไม่มี deptCode
                    })
                    .FirstOrDefaultAsync();

                if (shift == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลกะ" });
                }

                return Json(new { success = true, data = shift });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetShiftHistoryById: {Message}", ex.Message);
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message }
                );
            }
        }

        // เพิ่ม Method สำหรับดึงข้อมูลพื้นฐานของพนักงาน
        [HttpGet]
        public async Task<IActionResult> GetEmployeeBasicInfo(string personId)
        {
            try
            {
                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                // ดึงข้อมูลพนักงานจากฐานข้อมูลหลัก
                var employee = await _context
                    .emp_person.Where(e => e.personID.StartsWith(basePersonId))
                    .Select(e => new
                    {
                        personID = basePersonId,
                        name = e.name,
                        deptID = e.deptID,
                        deptName = e.deptName,
                        deptCode = e.deptID, // ใช้ deptID เป็น deptCode
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    // ถ้าไม่พบในฐานข้อมูลหลัก ให้ลองดึงจาก CES941
                    using (var connection = new SqlConnection(_ces941ConnectionString))
                    {
                        var query =
                            @"
                            SELECT 
                                EmpNo AS personID, 
                                CONCAT(PrefixName, ' ', EmpName, ' ', EmpLName) AS name,
                                DeptID AS deptID,
                                DeptDesc AS deptName,
                                DeptID AS deptCode
                            FROM MEmpBasic 
                            WHERE EmpNo = @basePersonId AND Language = 'EN'";

                        employee = await connection.QueryFirstOrDefaultAsync(
                            query,
                            new { basePersonId }
                        );
                    }

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }
                }

                return Json(new { success = true, data = employee });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEmployeeBasicInfo: {Message}", ex.Message);
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveResignedAccess()
        {
            try
            {
                // ดึงข้อมูลพนักงานที่ลาออกแล้วจาก CES941
                var ces941Employees = await GetEmployeesFromCES941(ViewData["CurrentUserDepartment"]?.ToString() ?? "");
                var resignedEmployees = ces941Employees
                    .Where(e => e.EmpResignDate != null)
                    .Select(e => e.EmpNo?.ToString())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();

                if (resignedEmployees.Count == 0)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานที่ลาออก" });
                }

                int successCount = 0;
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    foreach (var empNo in resignedEmployees)
                    {
                        // ลบสิทธิ์การเข้าประตูของพนักงานที่ลาออก (ทั้ง -1 และ -2)
                        var updateSql =
                            @"
                            UPDATE PubDoorAuth 
                            SET reserve1 = 2,
                                modifyTime = @modifyTime
                            WHERE personID IN (@personId1, @personId2)";

                        var result = await sql944Connection.ExecuteAsync(
                            updateSql,
                            new
                            {
                                modifyTime = DateTime.Now,
                                personId1 = empNo + "-1",
                                personId2 = empNo + "-2",
                            }
                        );

                        if (result > 0)
                        {
                            successCount++;
                        }
                    }
                }

                return Json(
                    new
                    {
                        success = true,
                        message = $"ลบสิทธิ์การเข้าประตูของพนักงานที่ลาออกเรียบร้อยแล้ว ({successCount} คน)",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PermissionDoorReport()
        {
            try
            {
                string doorId = Convert.ToString(Request.Query["doorId"]);
                string departmentId = Convert.ToString(Request.Query["departmentId"]);
                string keyword = Convert.ToString(Request.Query["keyword"]);

                // ดึงรายชื่อประตูสำหรับฟิลเตอร์
                using (var sql944 = new SqlConnection(_sql944ConnectionString))
                {
                    var doors = await sql944.QueryAsync<dynamic>(
                        @"SELECT DISTINCT doorID, doorName FROM PubDoor ORDER BY doorName"
                    );
                    ViewBag.Doors = doors;
                }

                // ดึงรายชื่อแผนกจากตาราง emp_person (เฉพาะค่าที่มีจริง)
                var departments = await _context.emp_person
                    .Where(e => e.deptID != null && e.deptName != null)
                    .Select(e => new { DepartmentId = e.deptID, DepartmentName = e.deptName })
                    .Distinct()
                    .OrderBy(e => e.DepartmentName)
                    .ToListAsync();
                ViewBag.Departments = departments;

                // ดึงข้อมูลสิทธิ์ประตูจาก SQL944
                IEnumerable<dynamic> rows;
                using (var sql944 = new SqlConnection(_sql944ConnectionString))
                {
                    var sql = @"SELECT TOP 1000 a.personID, a.doorID, d.doorName, a.weekTimeID, a.reserve1, a.modifyTime
                                FROM PubDoorAuth a
                                INNER JOIN PubDoor d ON a.doorID = d.doorID
                                WHERE (a.reserve1 IS NULL OR a.reserve1 <> 2)";
                    if (!string.IsNullOrEmpty(doorId))
                    {
                        sql += " AND a.doorID = @doorId";
                    }
                    sql += " ORDER BY d.doorName, a.personID";

                    rows = await sql944.QueryAsync<dynamic>(sql, new { doorId });
                }

                // สร้างแผนที่ข้อมูลพนักงานจาก emp_person แบบ cache
                var model = new List<dynamic>();
                var personCache = new Dictionary<string, (string Name, string DeptName, string DeptId)>();

                foreach (var r in rows)
                {
                    string personId = (string)r.personID;
                    if (string.IsNullOrWhiteSpace(personId)) continue;
                    string baseId = personId.Split('-')[0];

                    if (!personCache.ContainsKey(baseId))
                    {
                        var person = await _context.emp_person
                            .AsNoTracking()
                            .Where(e => e.personID.StartsWith(baseId))
                            .OrderBy(e => e.personID)
                            .Select(e => new { e.name, e.deptName, e.deptID })
                            .FirstOrDefaultAsync();

                        string name = person?.name ?? baseId;
                        string deptName = person?.deptName ?? string.Empty;
                        string deptCode = person?.deptID ?? string.Empty;
                        personCache[baseId] = (name, deptName, deptCode);
                    }

                    var info = personCache[baseId];

                    // กรองตาม department และ keyword ถ้ามี
                    if (!string.IsNullOrEmpty(departmentId))
                    {
                        if (!(info.DeptName?.Contains(departmentId, StringComparison.OrdinalIgnoreCase) == true
                              || info.DeptId?.Contains(departmentId, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        if (!(baseId.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                              || (info.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)))
                        {
                            continue;
                        }
                    }

                    model.Add(new
                    {
                        EmployeeCode = baseId,
                        EmployeeName = info.Name,
                        DepartmentName = info.DeptName,
                        DoorName = (string)r.doorName,
                        IsAllowed = true,
                        ValidFrom = (DateTime?)null,
                        ValidTo = (DateTime?)r.modifyTime
                    });
                }

                await LoadPermissions("Employee", "PermissionDoorReport");
                return View("permission_door_report", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PermissionDoorReport: {ex.Message}");
                return View("permission_door_report", new List<dynamic>());
            }
        }

        private async Task<string> GetPermissionDoorReportHtml()
        {
            var html = await System.IO.File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Views", "Employee", "permission_door_report.cshtml"));
            return html;
        }
    }
}
