using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPS_TH.Data;
using IPS_TH.Extensions; // เพิ่ม namespace สำหรับ Extension Methods
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IPS_TH.Controllers.Report
{
    //public class ReportController : BaseController
    //{
    //    private readonly string _connectionString;

    //    private readonly string _hrIpsConnectionString;

    //    private readonly IConfiguration _configuration;

    //    // sql944
    //    private readonly string _sql944ConnectionString;

    //    // เพิ่มตัวแปรสำหรับเชื่อมต่อกับ CES941
    //    private readonly string _ces941ConnectionString;

    //    public ReportController(IConfiguration configuration, ApplicationDbContext context )
    //        : base(context)
    //    {
    //        _connectionString = _context.Database.GetDbConnection().ConnectionString;
    //        _configuration = configuration;
    //        _sql944ConnectionString = _configuration.GetConnectionString("SQL944");
    //        _hrIpsConnectionString = _configuration.GetConnectionString("HR_IPS");
    //        _ces941ConnectionString = _configuration.GetConnectionString("CES941");
    //    }

    //    public IActionResult Report_Request()
    //    {
    //        LoadPermissions("Report", "Report_Request");

    //        // ดึงข้อมูลแผนกทั้งหมด
    //        ViewBag.Departments = GetDepartments();

    //        // ดึงข้อมูลกะการทำงานทั้งหมด
    //        ViewBag.Shifts = GetShifts();

    //        return View();
    //    }

    //    [HttpGet]
    //    [Route("Report/GetEmployees")]
    //    public async Task<IActionResult> GetEmployees(
    //        string search = "",
    //        string status = "",
    //        string department = "",
    //        string shift = ""
    //    )
    //    {
    //        try
    //        {
    //            int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
    //            int start = int.Parse(Request.Query["start"].FirstOrDefault() ?? "0");
    //            int length = int.Parse(Request.Query["length"].FirstOrDefault() ?? "10");
    //            string searchValue = Request.Query["search[value]"].FirstOrDefault() ?? "";

    //            // ดึงข้อมูลพนักงานจาก CES941
    //            var allEmployees = await GetEmployeesFromCES941();
    //            var filteredEmployees = allEmployees.ToList();

    //            // การค้นหา
    //            if (!string.IsNullOrEmpty(searchValue))
    //            {
    //                string searchLower = searchValue.ToLower();
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    (e.EmpNo != null && e.EmpNo.ToString().Contains(searchLower)) ||
    //                    (e.EmpName != null && e.EmpName.ToString().ToLower().Contains(searchLower)) ||
    //                    (e.EmpLName != null && e.EmpLName.ToString().ToLower().Contains(searchLower)) ||
    //                    (e.DeptID != null && e.DeptID.ToString().ToLower().Contains(searchLower)) ||
    //                    (e.MobilePhone != null && e.MobilePhone.ToString().Contains(searchLower))
    //                ).ToList();
    //            }

    //            // กรองตามแผนก
    //            if (!string.IsNullOrEmpty(department))
    //            {
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    e.DeptID?.ToString().Contains(department) == true).ToList();
    //            }

    //            // กรองตามกะการทำงาน
    //            if (!string.IsNullOrEmpty(shift))
    //            {
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    e.ShiftCode?.ToString() == shift).ToList();
    //            }

    //            // กรองตามสถานะ
    //            if (!string.IsNullOrEmpty(status))
    //            {
    //                if (status.ToLower() == "active")
    //                {
    //                    // พนักงานที่ยังทำงานอยู่คือไม่มีวันที่ลาออก
    //                    filteredEmployees = filteredEmployees.Where(e => e.EmpResignDate == null).ToList();
    //                }
    //                else
    //                {
    //                    // พนักงานที่ลาออกแล้วคือมีวันที่ลาออก
    //                    filteredEmployees = filteredEmployees.Where(e => e.EmpResignDate != null).ToList();
    //                }
    //            }

    //            // จำนวนทั้งหมดหลังการกรอง
    //            var recordsFiltered = filteredEmployees.Count;

    //            // ข้อมูลที่จะแสดงในหน้าปัจจุบัน
    //            var pagedEmployees = filteredEmployees
    //                .OrderBy(e => e.EmpName)
    //                .Skip(start)
    //                .Take(length)
    //                .Select(e => {
    //                    // แปลงวันที่ให้อยู่ในรูปแบบที่เหมาะสม
    //                    string startDate = null;
    //                    if (e.EmpStartDate != null)
    //                    {
    //                        try
    //                        {
    //                            var date = Convert.ToDateTime(e.EmpStartDate);
    //                            startDate = date.ToString("dd/MM/yyyy");
    //                        }
    //                        catch
    //                        {
    //                            startDate = e.EmpStartDate?.ToString();
    //                        }
    //                    }

    //                    // กำหนดสถานะการทำงาน
    //                    int useCategory = e.EmpResignDate == null ? 1 : 0;

    //                    // สร้างข้อมูลที่จะส่งไปยัง DataTable
    //                    return new
    //                    {
    //                        personID = e.EmpNo?.ToString(),
    //                        name = $"{e.PrefixName} {e.EmpName} {e.EmpLName}".Trim(),
    //                        useCategory = useCategory,
    //                        deptID = e.DeptID?.ToString(),
    //                        deptName = e.DeptID?.ToString(),
    //                        cardNumber = e.PersonalID?.ToString(),
    //                        shiftName = e.ShiftCode?.ToString() ?? "-",
    //                        startDate = startDate,
    //                        phone = e.MobilePhone?.ToString()
    //                    };
    //                })
    //                .ToList();

    //            // ส่งข้อมูลกลับในรูปแบบที่ DataTable ต้องการ
    //            return Json(new
    //            {
    //                draw,
    //                recordsTotal = allEmployees.Count,
    //                recordsFiltered = recordsFiltered,
    //                data = pagedEmployees
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            return Json(new
    //            {
    //                draw = 1,
    //                recordsTotal = 0,
    //                recordsFiltered = 0,
    //                data = new List<dynamic>(),
    //                error = ex.Message
    //            });
    //        }
    //    }

    //    [HttpGet]
    //    [Route("Report/GetEmployeeStats")]
    //    public async Task<IActionResult> GetEmployeeStats(
    //        string department = "",
    //        string shift = "",
    //        string status = ""
    //    )
    //    {
    //        try
    //        {
    //            // ดึงข้อมูลพนักงานจาก CES941
    //            var allEmployees = await GetEmployeesFromCES941();
    //            var filteredEmployees = allEmployees.ToList();

    //            // กรองตามแผนก
    //            if (!string.IsNullOrEmpty(department))
    //            {
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    e.ShiftCode?.ToString().Contains(department) == true).ToList();
    //            }

    //            // กรองตามกะการทำงาน
    //            if (!string.IsNullOrEmpty(shift))
    //            {
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    e.ShiftCode?.ToString() == shift).ToList();
    //            }

    //            // กรองตามสถานะ
    //            if (!string.IsNullOrEmpty(status))
    //            {
    //                filteredEmployees = filteredEmployees.Where(e =>
    //                    status.ToLower() == "active" ? e.EmpResignDate == null : e.EmpResignDate != null).ToList();
    //            }

    //            // คำนวณสถิติพื้นฐาน
    //            int totalEmployees = filteredEmployees.Count;
    //            int activeEmployees = filteredEmployees.Count(e => e.EmpResignDate == null);
    //            int inactiveEmployees = filteredEmployees.Count(e => e.EmpResignDate != null);

    //            // คำนวณพนักงานตามกะการทำงาน
    //            var shiftStats = filteredEmployees
    //                .GroupBy(e => e.ShiftCode?.ToString() ?? "ไม่ระบุ")
    //                .Select(g => new
    //                {
    //                    shift = g.Key,
    //                    count = g.Count()
    //                })
    //                .OrderByDescending(d => d.count)
    //                .ToList();

    //            return Json(new
    //            {
    //                success = true,
    //                total = totalEmployees,
    //                active = activeEmployees,
    //                inactive = inactiveEmployees,
    //                shiftStats = shiftStats
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            return Json(new
    //            {
    //                success = false,
    //                message = ex.Message
    //            });
    //        }
    //    }

    //    private async Task<List<dynamic>> GetEmployeesFromCES941()
    //    {
    //        try
    //        {
    //            using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
    //            {
    //                // ดึงข้อมูลตัวอย่างเพื่อตรวจสอบโครงสร้างตาราง
    //                string checkSql = @"SELECT TOP 1 * FROM MEmpBasic WHERE Language = 'EN'";
    //                var sampleData = await db.QueryFirstAsync(checkSql);
    //                var columns = ((IDictionary<string, object>)sampleData).Keys.ToList();

    //                // สร้าง SQL query ตามคอลัมน์ที่มีอยู่จริง
    //                string sql = @"
    //                    WITH RankedEmployees AS (
    //                        SELECT
    //                            Language, EmpNo, PrefixName, EmpName, EmpLName, Gender,
    //                            EmpStartDate, EmpResignDate, LastDateAtten, AcceptRehired,
    //                            BirthDate, MobilePhone, PersonalID, eMailAdd";

    //                // เพิ่มคอลัมน์ ShiftCode ถ้ามี
    //                if (columns.Any(c => c.Equals("ShiftCode", StringComparison.OrdinalIgnoreCase)))
    //                {
    //                    sql += ", ShiftCode";
    //                }

    //                sql += @",
    //                            ROW_NUMBER() OVER (PARTITION BY EmpNo ORDER BY
    //                                CASE
    //                                    WHEN Language = 'TH' THEN 1
    //                                    WHEN Language = 'EN' THEN 2
    //                                    ELSE 3
    //                                END) as RowNum
    //                        FROM MEmpBasic
    //                    )
    //                    SELECT * FROM RankedEmployees WHERE RowNum = 1";

    //                var employees = await db.QueryAsync(sql);
    //                var employeeList = employees.ToList();

    //                // เพิ่ม ShiftCode ถ้าไม่มีในฐานข้อมูล
    //                if (!columns.Any(c => c.Equals("ShiftCode", StringComparison.OrdinalIgnoreCase)))
    //                {
    //                    foreach (var employee in employeeList)
    //                    {
    //                        ((IDictionary<string, object>)employee).Add("ShiftCode", "-");
    //                    }
    //                }

    //                return employeeList;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error fetching employees from CES941: {ex.Message}");
    //            Console.WriteLine($"Stack trace: {ex.StackTrace}");
    //            return CreateMockEmployees();
    //        }
    //    }

    //    private List<dynamic> CreateMockEmployees()
    //    {
    //        var mockEmployees = new List<dynamic>();
    //        for (int i = 1; i <= 10; i++)
    //        {
    //            dynamic employee = new System.Dynamic.ExpandoObject();
    //            employee.EmpNo = $"EMP{i:000}";
    //            employee.PrefixName = "คุณ";
    //            employee.EmpName = $"ทดสอบ{i}";
    //            employee.EmpLName = "นามสกุล";
    //            employee.ShiftCode = "-";
    //            employee.EmpStartDate = DateTime.Now.AddYears(-1);
    //            employee.EmpResignDate = i % 3 == 0 ? (DateTime?)DateTime.Now.AddMonths(-1) : null;
    //            employee.MobilePhone = $"099{i:000000}";
    //            employee.PersonalID = $"1{i:00000000000}";
    //            employee.Gender = i % 2 == 0 ? "M" : "F";
    //            employee.BirthDate = DateTime.Now.AddYears(-25 - i);
    //            employee.eMailAdd = $"test{i}@example.com";

    //            mockEmployees.Add(employee);
    //        }
    //        return mockEmployees;
    //    }

    //    private List<dynamic> GetDepartments()
    //    {
    //        try
    //        {
    //            using (var connection = new SqlConnection(_sql944ConnectionString))
    //            {
    //                connection.Open();
    //                var sql = @"
    //                SELECT
    //                    deptID,
    //                    name,
    //                    Ext_No as extNo,
    //                    manager
    //                FROM Dept
    //                ORDER BY name";

    //                return connection.Query(sql).ToList();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error fetching departments: " + ex.Message);
    //            return new List<dynamic>();
    //        }
    //    }

    //    private List<dynamic> GetShifts()
    //    {
    //        try
    //        {
    //            using (var connection = new SqlConnection(_connectionString))
    //            {
    //                connection.Open();
    //                var sql = @"
    //                SELECT
    //                    id,
    //                    ShiftName,
    //                    ShiftGroup,
    //                    TimeIn,
    //                    TimeOut
    //                FROM emp_shift
    //                ORDER BY ShiftName";

    //                return connection.Query(sql).ToList();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error fetching shifts: " + ex.Message);
    //            return new List<dynamic>();
    //        }
    //    }

    //    [HttpPost]
    //    public async Task<IActionResult> ExportReport(
    //        string reportType,
    //        string startDate,
    //        string endDate,
    //        bool selectAll,
    //        string department,
    //        string shift,
    //        string status,
    //        List<string> employees,
    //        string format
    //    )
    //    {
    //        try
    //        {
    //            string fileName = $"Report_{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}";
    //            string fileExtension = ".xlsx";
    //            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    //            switch (format.ToLower())
    //            {
    //                case "pdf":
    //                    fileExtension = ".pdf";
    //                    contentType = "application/pdf";
    //                    break;
    //                case "csv":
    //                    fileExtension = ".csv";
    //                    contentType = "text/csv";
    //                    break;
    //            }

    //            string filePath = Path.Combine(Path.GetTempPath(), fileName + fileExtension);
    //            var reportData = await GetReportData(reportType, startDate, endDate, selectAll, department, shift, status, employees);
    //            string fileUrl = "";

    //            switch (format.ToLower())
    //            {
    //                case "excel":
    //                    fileUrl = await CreateExcelReport(reportType, reportData, filePath);
    //                    break;
    //                case "pdf":
    //                    fileUrl = await CreatePdfReport(reportType, reportData, filePath);
    //                    break;
    //                case "csv":
    //                    fileUrl = await CreateCsvReport(reportType, reportData, filePath);
    //                    break;
    //            }

    //            return Json(new
    //            {
    //                success = true,
    //                fileUrl = fileUrl
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            return Json(new
    //            {
    //                success = false,
    //                message = ex.Message
    //            });
    //        }
    //    }

    //    private async Task<List<dynamic>> GetReportData(
    //        string reportType,
    //        string startDate,
    //        string endDate,
    //        bool selectAll,
    //        string department,
    //        string shift,
    //        string status,
    //        List<string> employees
    //    )
    //    {
    //        List<dynamic> mockData = new List<dynamic>();

    //        for (int i = 1; i <= 10; i++)
    //        {
    //            dynamic item = new System.Dynamic.ExpandoObject();
    //            item.EmployeeId = $"EMP-{i:D3}-1";
    //            item.EmployeeName = $"พนักงานทดสอบ {i}";
    //            item.Department = "แผนกทดสอบ";
    //            item.Date = DateTime.Now.AddDays(-i).ToString("dd/MM/yyyy");
    //            item.CheckIn = $"{8 + (i % 2):D2}:00";
    //            item.CheckOut = $"{17 + (i % 2):D2}:00";
    //            item.Status = i % 3 == 0 ? "มาสาย" : "ปกติ";

    //            mockData.Add(item);
    //        }

    //        return mockData;
    //    }

    //    private async Task<string> CreateExcelReport(string reportType, List<dynamic> data, string filePath)
    //    {
    //        // เราจะต้องเพิ่มแพ็คเกจ EPPlus เพื่อสร้าง Excel file ในโปรเจค
    //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    //        using (var package = new ExcelPackage())
    //        {
    //            var worksheet = package.Workbook.Worksheets.Add("Report");

    //            // ตั้งค่าหัวข้อรายงาน
    //            string reportTitle = "รายงานข้อมูลพนักงาน";

    //            switch (reportType)
    //            {
    //                case "attendance":
    //                    reportTitle = "รายงานการเข้างาน";
    //                    break;
    //                case "summary":
    //                    reportTitle = "รายงานสรุปการทำงาน";
    //                    break;
    //                case "overtime":
    //                    reportTitle = "รายงานการทำงานล่วงเวลา";
    //                    break;
    //                case "leave":
    //                    reportTitle = "รายงานการลา";
    //                    break;
    //                case "late":
    //                    reportTitle = "รายงานการมาสาย";
    //                    break;
    //            }

    //            // สร้างหัวตาราง
    //            worksheet.Cells[1, 1].Value = reportTitle;
    //            worksheet.Cells[1, 1, 1, 7].Merge = true;
    //            worksheet.Cells[1, 1, 1, 7].Style.Font.Size = 16;
    //            worksheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
    //            worksheet.Cells[1, 1, 1, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

    //            // ข้อมูลวันที่ออกรายงาน
    //            worksheet.Cells[2, 1].Value = $"วันที่ออกรายงาน: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
    //            worksheet.Cells[2, 1, 2, 7].Merge = true;
    //            worksheet.Cells[2, 1, 2, 7].Style.Font.Size = 12;
    //            worksheet.Cells[2, 1, 2, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

    //            // หัวข้อตาราง
    //            var headers = new List<string> { "รหัสพนักงาน", "ชื่อ-นามสกุล", "แผนก", "วันที่", "เวลาเข้างาน", "เวลาออกงาน", "สถานะ" };

    //            for (int i = 0; i < headers.Count; i++)
    //            {
    //                worksheet.Cells[4, i + 1].Value = headers[i];
    //                worksheet.Cells[4, i + 1].Style.Font.Bold = true;
    //                worksheet.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
    //                worksheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
    //                worksheet.Cells[4, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
    //            }

    //            // เพิ่มข้อมูล
    //            for (int i = 0; i < data.Count; i++)
    //            {
    //                worksheet.Cells[i + 5, 1].Value = data[i].EmployeeId;
    //                worksheet.Cells[i + 5, 2].Value = data[i].EmployeeName;
    //                worksheet.Cells[i + 5, 3].Value = data[i].Department;
    //                worksheet.Cells[i + 5, 4].Value = data[i].Date;
    //                worksheet.Cells[i + 5, 5].Value = data[i].CheckIn;
    //                worksheet.Cells[i + 5, 6].Value = data[i].CheckOut;
    //                worksheet.Cells[i + 5, 7].Value = data[i].Status;

    //                for (int j = 1; j <= 7; j++)
    //                {
    //                    worksheet.Cells[i + 5, j].Style.Border.BorderAround(ExcelBorderStyle.Thin);
    //                }
    //            }

    //            // ปรับขนาดคอลัมน์อัตโนมัติ
    //            worksheet.Cells.AutoFitColumns();

    //            // บันทึกไฟล์
    //            var fileInfo = new FileInfo(filePath);
    //            package.SaveAs(fileInfo);
    //        }

    //        // สร้าง URL สำหรับดาวน์โหลดไฟล์
    //        string virtualFilePath = $"/temp/{Path.GetFileName(filePath)}";
    //        string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");

    //        if (!Directory.Exists(physicalPath))
    //        {
    //            Directory.CreateDirectory(physicalPath);
    //        }

    //        System.IO.File.Copy(filePath, Path.Combine(physicalPath, Path.GetFileName(filePath)), true);

    //        return virtualFilePath;
    //    }

    //    private async Task<string> CreatePdfReport(string reportType, List<dynamic> data, string filePath)
    //    {
    //        // สร้าง HTML สำหรับ PDF
    //        StringBuilder html = new StringBuilder();

    //        html.Append(@"
    //        <!DOCTYPE html>
    //        <html>
    //        <head>
    //            <meta charset='UTF-8'>
    //            <title>Report</title>
    //            <style>
    //                body { font-family: 'Sarabun', sans-serif; }
    //                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
    //                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    //                th { background-color: #f2f2f2; }
    //                .header { text-align: center; margin-bottom: 20px; }
    //                .header h1 { margin-bottom: 5px; }
    //                .header p { margin-top: 0; color: #666; }
    //            </style>
    //        </head>
    //        <body>");

    //        // ตั้งค่าหัวข้อรายงาน
    //        string reportTitle = "รายงานข้อมูลพนักงาน";

    //        switch (reportType)
    //        {
    //            case "attendance":
    //                reportTitle = "รายงานการเข้างาน";
    //                break;
    //            case "summary":
    //                reportTitle = "รายงานสรุปการทำงาน";
    //                break;
    //            case "overtime":
    //                reportTitle = "รายงานการทำงานล่วงเวลา";
    //                break;
    //            case "leave":
    //                reportTitle = "รายงานการลา";
    //                break;
    //            case "late":
    //                reportTitle = "รายงานการมาสาย";
    //                break;
    //        }

    //        html.Append($@"
    //        <div class='header'>
    //            <h1>{reportTitle}</h1>
    //            <p>วันที่ออกรายงาน: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
    //        </div>

    //        <table>
    //            <thead>
    //                <tr>
    //                    <th>รหัสพนักงาน</th>
    //                    <th>ชื่อ-นามสกุล</th>
    //                    <th>แผนก</th>
    //                    <th>วันที่</th>
    //                    <th>เวลาเข้างาน</th>
    //                    <th>เวลาออกงาน</th>
    //                    <th>สถานะ</th>
    //                </tr>
    //            </thead>
    //            <tbody>");

    //        // เพิ่มข้อมูล
    //        foreach (var item in data)
    //        {
    //            html.Append($@"
    //            <tr>
    //                <td>{item.EmployeeId}</td>
    //                <td>{item.EmployeeName}</td>
    //                <td>{item.Department}</td>
    //                <td>{item.Date}</td>
    //                <td>{item.CheckIn}</td>
    //                <td>{item.CheckOut}</td>
    //                <td>{item.Status}</td>
    //            </tr>");
    //        }

    //        html.Append(@"
    //            </tbody>
    //        </table>
    //        </body>
    //        </html>");

    //        // ตรวจสอบว่ามี PDF converter หรือไม่
    //        if (_pdfConverter == null)
    //        {
    //            // ถ้าไม่มี DinkToPdf ให้สร้างเป็น HTML แทน
    //            string htmlFilePath = filePath.Replace(".pdf", ".html");
    //            await System.IO.File.WriteAllTextAsync(htmlFilePath, html.ToString());

    //            // สร้าง URL สำหรับดาวน์โหลดไฟล์
    //            string virtualFilePath = $"/temp/{Path.GetFileName(htmlFilePath)}";
    //            string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");

    //            if (!Directory.Exists(physicalPath))
    //            {
    //                Directory.CreateDirectory(physicalPath);
    //            }

    //            System.IO.File.Copy(htmlFilePath, Path.Combine(physicalPath, Path.GetFileName(htmlFilePath)), true);

    //            return virtualFilePath;
    //        }

    //        // สร้าง PDF ด้วย DinkToPdf
    //        var doc = new HtmlToPdfDocument()
    //        {
    //            GlobalSettings = {
    //                ColorMode = ColorMode.Color,
    //                Orientation = Orientation.Portrait,
    //                PaperSize = PaperKind.A4,
    //                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
    //                Out = filePath
    //            },
    //            Objects = {
    //                new ObjectSettings {
    //                    HtmlContent = html.ToString(),
    //                    WebSettings = { DefaultEncoding = "utf-8" }
    //                }
    //            }
    //        };

    //        _pdfConverter.Convert(doc);

    //        // สร้าง URL สำหรับดาวน์โหลดไฟล์
    //        string vFilePath = $"/temp/{Path.GetFileName(filePath)}";
    //        string pPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");

    //        if (!Directory.Exists(pPath))
    //        {
    //            Directory.CreateDirectory(pPath);
    //        }

    //        System.IO.File.Copy(filePath, Path.Combine(pPath, Path.GetFileName(filePath)), true);

    //        return vFilePath;
    //    }

    //    private async Task<string> CreateCsvReport(string reportType, List<dynamic> data, string filePath)
    //    {
    //        // สร้างข้อมูล CSV
    //        StringBuilder csv = new StringBuilder();

    //        // หัวข้อตาราง
    //        csv.AppendLine("รหัสพนักงาน,ชื่อ-นามสกุล,แผนก,วันที่,เวลาเข้างาน,เวลาออกงาน,สถานะ");

    //        // เพิ่มข้อมูล
    //        foreach (var item in data)
    //        {
    //            csv.AppendLine($"{item.EmployeeId},{item.EmployeeName},{item.Department},{item.Date},{item.CheckIn},{item.CheckOut},{item.Status}");
    //        }

    //        // บันทึกไฟล์
    //        await System.IO.File.WriteAllTextAsync(filePath, csv.ToString());

    //        // สร้าง URL สำหรับดาวน์โหลดไฟล์
    //        string virtualFilePath = $"/temp/{Path.GetFileName(filePath)}";
    //        string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");

    //        if (!Directory.Exists(physicalPath))
    //        {
    //            Directory.CreateDirectory(physicalPath);
    //        }

    //        System.IO.File.Copy(filePath, Path.Combine(physicalPath, Path.GetFileName(filePath)), true);

    //        return virtualFilePath;
    //    }
    //}
}
