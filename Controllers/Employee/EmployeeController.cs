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
        private readonly string _connectionString;

        private readonly string _hrIpsConnectionString;

        private readonly IConfiguration _configuration;

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
                {
                    var query = @"SELECT * FROM Dept";

                    // ถ้าไม่มีสิทธิ์ edit ให้แสดงเฉพาะแผนกของผู้ใช้
                    // if (!CheckPermission("Edit"))
                    // {
                    //     query += " WHERE name = @userDept";
                    // }

                    // เพิ่มข้อมูล Shiftdept
                    var shiftDeptQuery =
                        @"SELECT * FROM emp_shift WHERE record_status = 'N' ORDER BY shift_group";
                    var shiftDept = await defaultconnection.QueryAsync<dynamic>(shiftDeptQuery);
                    ViewBag.Shiftdept = shiftDept;

                    var dept = await connection.QueryAsync<dynamic>(query);
                    ViewBag.Dept = dept;

                    // เพิ่มการดึงข้อมูลประตู
                    var doorsSql =
                        @"SELECT DISTINCT doorID, doorName 
                        FROM PubDoor 
                        ORDER BY doorName";
                    var doors = await connection.QueryAsync<dynamic>(doorsSql);
                    ViewBag.Doors = doors;
                }

                LoadPermissions("Employee", "Employee");
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
        public async Task<IActionResult> DeleteEmployee(string personId)
        {
            try
            {
                // ดึงข้อมูลพนักงาน
                var basePersonId = personId.Split('-')[0];

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (var hrConnection = new SqlConnection(_hrIpsConnectionString))
                {
                    // ลบข้อมูลจาก SQL944
                    await sql944Connection.ExecuteAsync(
                        @"
                        DELETE FROM Person 
                        WHERE PersonID IN (@personId1, @personId2)",
                        new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                    );

                    // ลบข้อมูลจาก PubDoorAuth ใน SQL944
                    await sql944Connection.ExecuteAsync(
                        @"
                        DELETE FROM PubDoorAuth 
                        WHERE PersonID IN (@personId1, @personId2)",
                        new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                    );

                    // ลบข้อมูลจาก HR_IPS
                    await hrConnection.ExecuteAsync(
                        @"
                        DELETE FROM emp_person 
                        WHERE personID IN (@personId1, @personId2)",
                        new { personId1 = basePersonId + "-1", personId2 = basePersonId + "-2" }
                    );

                    // ลบข้อมูลจากฐานข้อมูลหลัก
                    await _context.Database.ExecuteSqlRawAsync(
                        @"
                        DELETE FROM emp_person 
                        WHERE personID IN ({0}, {1})",
                        basePersonId + "-1",
                        basePersonId + "-2"
                    );

                    // ลบข้อมูลจาก emp_person_shift ถ้ามี
                    await _context.Database.ExecuteSqlRawAsync(
                        @"
                        DELETE FROM emp_person_shift 
                        WHERE personID = {0}",
                        basePersonId
                    );

                    return Json(new { success = true, message = "ลบข้อมูลเรียบร้อยแล้ว" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
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
        private async Task<List<dynamic>> GetEmployeesFromCES941()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    string sql =
                        @"
                        SELECT 
                            Language, EmpNo, PrefixName, EmpName, EmpLName, Gender, 
                            EmpStartDate, EmpResignDate, LastDateAtten, AcceptRehired, 
                            ProbationDay, ProbationPass, ProbationDate, ContractNo, 
                            ContAdd, ContCity, ContZipCode, ContZipDesc, ContCountry, 
                            RegAdd, RegCity, RegZipCode, RegZipDesc, RegCountry, 
                            HomePhone, MobilePhone, Internet, eMailAdd, ADUser, 
                            R3User, DIHUser, LotusUser, SkyUser, RNewUser, BirthDate, 
                            BirthCity, BirthCountry, Nation, Nationality, Religion, 
                            BankID, BankAccount, PersonalID, PassPort, TaxID, SFID, 
                            Hospital, CarLicense, BikeLicense, Marital, MilitaryPass, 
                            EmpPicture, DocLink, PreEmpNo, ChangeDate, ChangeBy, 
                            FlagDel, AdminAction, ResignAlertDate, ResignKeyDate, Ext, ADPath
                        FROM MEmpBasic
                        WHERE Language = 'EN'";

                    // ย้ายเงื่อนไขการกรองตามสถานะการลาออกไปใช้ในตอนกรองข้อมูลหลังจากเปรียบเทียบแล้ว
                    // ดึงข้อมูลทั้งหมดโดยไม่กรองเพื่อให้การเปรียบเทียบครบถ้วน

                    var employees = await db.QueryAsync(sql);
                    return employees.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching employees from CES941: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // ปรับปรุงเมธอด GetEmployees เพื่อดึงข้อมูลจาก CES941 และ SQL944
        [HttpGet]
        [Route("Employee/GetEmployeesData")]
        public async Task<IActionResult> GetEmployees(
            string status = "",
            string door = "",
            string department = "",
            string shift = "",
            string fingerprint = ""
        )
        {
            try
            {
                // ดึงข้อมูลพนักงานจาก CES941
                var ces941Employees = await GetEmployeesFromCES941();

                // สร้าง Dictionary สำหรับพนักงานจาก CES941
                var ces941Dict = new Dictionary<string, dynamic>();
                foreach (var emp in ces941Employees)
                {
                    string id = emp.EmpNo?.ToString().Trim();
                    if (!string.IsNullOrEmpty(id))
                    {
                        ces941Dict[id] = emp;
                    }
                }

                // ดึงข้อมูลพนักงานจาก SQL944
                var sql944Employees = new HashSet<string>();
                var cardDict = new Dictionary<string, dynamic>();
                var fingerprintDict = new Dictionary<string, dynamic>();
                var nameDict = new Dictionary<string, string>();
                var deptNameDict = new Dictionary<string, string>();
                var deptIDDict = new Dictionary<string, string>();
                var cardNumberDict = new Dictionary<string, string>(); // เพิ่ม Dictionary สำหรับ cardNumber
                var accessCountDict = new Dictionary<string, int>();
                var fingerprintDataIds = new HashSet<string>();

                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูลบัตร (-1) รวมทั้ง cardNumber
                    var cardSql =
                        @"SELECT personID, Name, DeptName, DeptID, CardNumber FROM Person WHERE personID LIKE '%-1'";
                    var cardData = await sql944Connection.QueryAsync<dynamic>(cardSql);
                    foreach (var card in cardData)
                    {
                        string baseId = card.personID.Split('-')[0].Trim();
                        sql944Employees.Add(baseId);
                        cardDict[baseId] = card;
                        if (!string.IsNullOrEmpty(card.Name))
                            nameDict[baseId] = card.Name;
                        if (!string.IsNullOrEmpty(card.DeptName))
                            deptNameDict[baseId] = card.DeptName;
                        if (!string.IsNullOrEmpty(card.DeptID))
                            deptIDDict[baseId] = card.DeptID;
                        // เก็บ cardNumber จาก SQL944
                        if (!string.IsNullOrEmpty(card.CardNumber))
                            cardNumberDict[baseId] = card.CardNumber;
                    }

                    // ดึงข้อมูลลายนิ้วมือ (-2)
                    var fingerprintSql =
                        @"SELECT personID, Name, DeptName, DeptID FROM Person WHERE personID LIKE '%-2'";
                    var fingerprintData = await sql944Connection.QueryAsync<dynamic>(
                        fingerprintSql
                    );
                    foreach (var fp in fingerprintData)
                    {
                        string baseId = fp.personID.Split('-')[0].Trim();
                        sql944Employees.Add(baseId);
                        fingerprintDict[baseId] = fp;
                        if (!string.IsNullOrEmpty(fp.Name) && !nameDict.ContainsKey(baseId))
                            nameDict[baseId] = fp.Name;
                        if (!string.IsNullOrEmpty(fp.DeptName) && !deptNameDict.ContainsKey(baseId))
                            deptNameDict[baseId] = fp.DeptName;
                        if (!string.IsNullOrEmpty(fp.DeptID) && !deptIDDict.ContainsKey(baseId))
                            deptIDDict[baseId] = fp.DeptID;
                    }

                    // ดึงข้อมูลสิทธิ์การเข้าประตู
                    var doorAccessSql =
                        @"SELECT personID, doorID FROM PubDoorAuth WHERE reserve1 != 2";
                    var doorAccessData = await sql944Connection.QueryAsync<dynamic>(doorAccessSql);
                    foreach (var access in doorAccessData)
                    {
                        string baseId = access.personID.Split('-')[0].Trim();
                        accessCountDict[baseId] = accessCountDict.GetValueOrDefault(baseId, 0) + 1;
                    }

                    // ดึงข้อมูลลายนิ้วมือ
                    var fingerprintDataSql =
                        @"SELECT PersonID FROM Person_FP WHERE (FP1 IS NOT NULL OR FP2 IS NOT NULL)";
                    var fingerprintRecords = await sql944Connection.QueryAsync<string>(
                        fingerprintDataSql
                    );
                    fingerprintDataIds = fingerprintRecords
                        .Select(id => id.Split('-')[0].Trim())
                        .ToHashSet();
                }

                // ดึงข้อมูลกะ
                var shifts = await _context
                    .emp_person_shift.Where(s => s.personID != null)
                    .GroupBy(s => s.personID)
                    .Select(g => new
                    {
                        PersonID = g.Key,
                        ShiftMent = g.OrderByDescending(s => s.date).FirstOrDefault().shiftMent,
                    })
                    .ToDictionaryAsync(k => k.PersonID, v => v.ShiftMent);

                // รวมข้อมูลพนักงานจากทั้งสองแหล่ง
                var employeesList = new List<dynamic>();
                var processedIds = new HashSet<string>();

                // ประมวลผลพนักงานจาก CES941
                foreach (var kvp in ces941Dict)
                {
                    string id = kvp.Key;
                    var emp = kvp.Value;
                    processedIds.Add(id); 

                    bool inSql944 = sql944Employees.Contains(id);
                    var cardData = cardDict.GetValueOrDefault(id);
                    var fingerprintData = fingerprintDict.GetValueOrDefault(id);

                    employeesList.Add(
                        new
                        {
                            personID = id,
                            name = nameDict.ContainsKey(id)
                            && !string.IsNullOrWhiteSpace(nameDict[id])
                                ? nameDict[id]
                                : $"{emp.PrefixName} {emp.EmpName} {emp.EmpLName}".Trim(),
                            department = deptNameDict.ContainsKey(id)
                            && !string.IsNullOrWhiteSpace(deptNameDict[id])
                                ? deptNameDict[id]
                                : emp.DeptName ?? "",
                            deptID = deptIDDict.ContainsKey(id)
                            && !string.IsNullOrWhiteSpace(deptIDDict[id])
                                ? deptIDDict[id]
                                : emp.DeptID ?? "",
                            position = emp.PositionName ?? "",
                            startDate = emp.EmpStartDate,
                            resignDate = emp.EmpResignDate,
                            phone = emp.MobilePhone ?? "",
                            email = emp.eMailAdd ?? "",
                            gender = emp.Gender ?? "",
                            cardNumber = cardNumberDict.GetValueOrDefault(id, ""), // ใช้ cardNumber จาก SQL944
                            hasCardData = cardDict.ContainsKey(id),
                            hasFingerprintData = fingerprintDict.ContainsKey(id),
                            accessCount = accessCountDict.GetValueOrDefault(id, 0),
                            shift = shifts.GetValueOrDefault(id, ""),
                            hasFingerprint = fingerprintDataIds.Contains(id),
                            isActive = (cardDict.ContainsKey(id) || fingerprintDict.ContainsKey(id))
                                ? 1
                                : 0,
                            existsInCES941 = true,
                            existsInSQL944 = inSql944,
                        }
                    );
                }

                // ประมวลผลพนักงานที่มีเฉพาะใน SQL944
                foreach (var id in sql944Employees)
                {
                    if (!processedIds.Contains(id))
                    {
                        var cardData = cardDict.GetValueOrDefault(id);
                        var fingerprintData = fingerprintDict.GetValueOrDefault(id);
                        var sql944Data = cardData ?? fingerprintData;

                        employeesList.Add(
                            new
                            {
                                personID = id,
                                name = nameDict.GetValueOrDefault(id, "ไม่ระบุชื่อ"),
                                department = deptNameDict.GetValueOrDefault(id, ""),
                                deptID = deptIDDict.GetValueOrDefault(id, ""),
                                position = "",
                                startDate = "",
                                resignDate = "",
                                phone = "",
                                email = "",
                                gender = "",
                                cardNumber = cardNumberDict.GetValueOrDefault(id, ""), // ใช้ cardNumber จาก SQL944
                                hasCardData = cardDict.ContainsKey(id),
                                hasFingerprintData = fingerprintDict.ContainsKey(id),
                                accessCount = accessCountDict.GetValueOrDefault(id, 0),
                                shift = shifts.GetValueOrDefault(id, ""),
                                hasFingerprint = fingerprintDataIds.Contains(id),
                                isActive = (
                                    cardDict.ContainsKey(id) || fingerprintDict.ContainsKey(id)
                                )
                                    ? 1
                                    : 0,
                                existsInCES941 = false,
                                existsInSQL944 = true,
                            }
                        );
                    }
                }

                // กรองตามเงื่อนไข
                if (!string.IsNullOrEmpty(status))
                {
                    employeesList = status switch
                    {
                        "1" => employeesList
                            .Where(e => ((dynamic)e).hasCardData || ((dynamic)e).hasFingerprintData)
                            .ToList(),
                        "0" => employeesList
                            .Where(e =>
                                !((dynamic)e).hasCardData && !((dynamic)e).hasFingerprintData
                            )
                            .ToList(),
                        "incomplete" => employeesList
                            .Where(e =>
                                !((dynamic)e).existsInCES941 || !((dynamic)e).existsInSQL944
                            )
                            .ToList(),
                        "ces941only" => employeesList
                            .Where(e => ((dynamic)e).existsInCES941 && !((dynamic)e).existsInSQL944)
                            .ToList(),
                        "sql944only" => employeesList
                            .Where(e => !((dynamic)e).existsInCES941 && ((dynamic)e).existsInSQL944)
                            .ToList(),
                        "active" => employeesList
                            .Where(e => ((dynamic)e).resignDate == null)
                            .ToList(),
                        "resigned" => employeesList
                            .Where(e => ((dynamic)e).resignDate != null)
                            .ToList(),
                        _ => employeesList,
                    };
                }

                if (!string.IsNullOrEmpty(door) && door != "all")
                {
                    using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                    {
                        var doorAccessSql =
                            @"SELECT DISTINCT personID FROM PubDoorAuth WHERE doorID = @doorID AND reserve1 != 2";
                        var authorizedPersons = await sql944Connection.QueryAsync<string>(
                            doorAccessSql,
                            new { doorID = door }
                        );
                        var basePersonIds = authorizedPersons
                            .Select(p => p.Split('-')[0].Trim())
                            .ToHashSet();
                        employeesList = employeesList
                            .Where(e => basePersonIds.Contains(((dynamic)e).personID))
                            .ToList();
                    }
                }

                if (!string.IsNullOrEmpty(department))
                    employeesList = employeesList
                        .Where(e => ((dynamic)e).deptID == department)
                        .ToList();

                if (!string.IsNullOrEmpty(shift))
                    employeesList = employeesList.Where(e => ((dynamic)e).shift == shift).ToList();

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

                return Json(new { success = true, data = employeesList });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployees: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
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
                    if (sql944Employee == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

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
                var ces941Employees = await GetEmployeesFromCES941();

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
                    e.EmpStartDate != null
                    && DateTime.Parse(e.EmpStartDate.ToString()) >= firstDayOfMonth
                );

                // คำนวณพนักงานลาออกเดือนนี้
                int resignedEmployees = ces941Employees.Count(e =>
                    e.EmpResignDate != null
                    && DateTime.Parse(e.EmpResignDate.ToString()) >= firstDayOfMonth
                );

                // ดึงข้อมูลพนักงานที่มีวันเกิดวันนี้
                var birthdayEmployees = ces941Employees
                    .Where(e =>
                    {
                        // เฉพาะพนักงานที่ยังทำงานอยู่ (ไม่มีวันลาออก)
                        if (e.BirthDate == null || e.EmpResignDate != null)
                            return false;

                        var birthDate = DateTime.Parse(e.BirthDate.ToString());
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
                    .Select(e =>
                    {
                        // คำนวณอายุ
                        var birthDate = DateTime.Parse(e.BirthDate.ToString());
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
                        var startDate = DateTime.Parse(e.EmpStartDate.ToString());
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
                        e.EmpStartDate != null
                        && DateTime.Parse(e.EmpStartDate.ToString()) >= firstDay
                        && DateTime.Parse(e.EmpStartDate.ToString()) <= lastDay
                    );

                    var resignedCount = ces941Employees.Count(e =>
                        e.EmpResignDate != null
                        && DateTime.Parse(e.EmpResignDate.ToString()) >= firstDay
                        && DateTime.Parse(e.EmpResignDate.ToString()) <= lastDay
                    );

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

                        var resignDate = DateTime.Parse(e.EmpResignDate.ToString());
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
                        var startDate = DateTime.Parse(e.EmpStartDate.ToString());
                        var resignDate = DateTime.Parse(e.EmpResignDate.ToString());
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
                                a.doorID == door.doorID && a.reserve1 != 2
                            )
                                ? 1
                                : 0,
                            accessFinger = fingerAccess.Any(a =>
                                a.doorID == door.doorID && a.reserve1 != 2
                            )
                                ? 1
                                : 0,
                            hasCardAccess = cardAccess.Any(a =>
                                a.doorID == door.doorID && a.reserve1 != 2
                            ),
                            hasFingerprintAccess = fingerAccess.Any(a =>
                                a.doorID == door.doorID && a.reserve1 != 2
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
                        SELECT doorID, weekTimeID 
                        FROM PubDoor 
                        LEFT JOIN PubWeekTime ON PubDoor.contolL1 = PubWeekTime.contolL1"
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
                        WHERE doorName LIKE '%Build1 - HR Attendance%' 
                        OR doorName LIKE '%Comsumables room%'"
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
                var personIdValue = personId.Split('-')[0];
                personIdValue = personIdValue + "-1";
                var cardNumber = "";
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    await sql944Connection.OpenAsync();
                    var person = await sql944Connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT TOP 1 * FROM Person WHERE PersonID = @PersonID",
                        new { PersonID = personIdValue }
                    );
                    if (person == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }
                    cardNumber = person.cardNumber;
                }

                // ดึงข้อมูลจาก emp_person_shift โดยไม่ใช้ฟิลด์ที่ยังไม่มีในฐานข้อมูล
                var history = await _context
                    .emp_person_shift.Where(h => h.personID == personId && h.isActive == 1)
                    .OrderByDescending(h => h.date) // เรียงตามวันที่ล่าสุด
                    .Select(h => new
                    {
                        id = h.Id,
                        date = h.date,
                        shiftName = h.shiftMent,
                        shiftMent = h.shiftMent,
                        personID = personId,
                        name = h.name,
                        deptID = h.deptID,
                        deptName = h.deptName,
                        deptCode = h.deptCode,
                        cardNumber = cardNumber,
                    })
                    .ToListAsync();

                // ส่งคืนข้อมูลประวัติ
                return Json(new { success = true, data = history });
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
                        // หาข้อมูลพนักงาน
                        var employee = await _context.emp_person.FirstOrDefaultAsync(p =>
                            p.personID == request.PersonId
                        );

                        if (employee == null)
                        {
                            return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                        }

                        // กำหนดค่าข้อมูลที่จำเป็น
                        request.name = request.name ?? FormatEmployeeName(employee.name); // ใช้ชื่อที่ตัดแล้ว
                        request.deptID = request.deptID ?? employee.deptID;
                        request.deptName = request.deptName ?? employee.deptName;
                        request.deptCode = request.deptCode ?? employee.deptID;
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
                // ใช้ async/await เพื่อรอให้การโหลด permissions เสร็จสมบูรณ์
                LoadPermissions("Employee", "TypeShift");

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
                var ces941Employees = await GetEmployeesFromCES941();
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
            string name = System.Text.RegularExpressions.Regex.Replace(fullName, 
                @"^(Mr\.|Mrs\.|Miss|Ms\.|Dr\.|Prof\.)\s+", "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
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
                var ces941Employees = await GetEmployeesFromCES941();
                var employee = ces941Employees.FirstOrDefault(e => e.EmpNo?.ToString() == personId);

                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานใน CES941" });
                }

                // สร้างข้อมูลพนักงานสำหรับบันทึกลง SQL944
                var fingerprintPersonId = $"{personId}-2"; // รูปแบบสำหรับลายนิ้วมือ

                // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่
                using (var sql944Connection = new SqlConnection(_sql944ConnectionString))
                {
                    var existingPerson = await sql944Connection.QueryFirstOrDefaultAsync(
                        "SELECT TOP 1 1 FROM Person WHERE PersonID = @PersonID",
                        new { PersonID = fingerprintPersonId }
                    );

                    if (existingPerson != null)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "มีข้อมูลลายนิ้วมือของพนักงานนี้อยู่แล้ว",
                            }
                        );
                    }

                    // บันทึกข้อมูลลง SQL944
                    var insertSql =
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

                    await sql944Connection.ExecuteAsync(
                        insertSql,
                        new
                        {
                            CardNumber = cardNumber,
                            PersonID = fingerprintPersonId,
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
                var ces941Employees = await GetEmployeesFromCES941();

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
                    if (access.reserve1 != 2)
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
                            : (string.IsNullOrEmpty(ces941Name) ? "ไม่ระบุชื่อ" : ces941Name);

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
                    string employeeName = "ไม่ระบุชื่อ";
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

                    employees.Add(employee);
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
                                AND reserve1 != 2"; // เฉพาะที่ยังไม่ถูกยกเลิกสิทธิ์

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
                var ces941Employees = await GetEmployeesFromCES941();
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
    }
}
