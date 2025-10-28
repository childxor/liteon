using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
// using IPS_TH.Extensions; // Comment out to avoid conflict
using IPS_TH.Models.AssetFactory;
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;

namespace IPS_TH.Controllers.AssetFactory
{
    public class AssetController : BaseController
    {
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _ces941ConnectionString;
        private readonly string _oracleConnectionString;

        public AssetController(IConfiguration configuration, ApplicationDbContext context)
            : base(context)
        {
            _dbContext = context;
            _connectionString = context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = configuration.GetConnectionString("CES941");
            _oracleConnectionString = configuration.GetConnectionString("OracleConnection");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                await LoadPermissions("Asset", "Index");
                return View("~/Views/AssetFactory/Asset.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Asset action: {ex.Message}");
                return View("~/Views/AssetFactory/Asset.cshtml");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                // ดึงข้อมูลจากตาราง Asset พร้อมกับ Mac Address
                var query =
                    @"
                    SELECT  
                       a.Id, a.ProductSerial, a.AssetNo, a.CustomerNo, a.CustomerName, a.Department, 
                       a.DepartmentName, a.Line, a.LineName, a.Owner, a.OwnerName, a.Name, a.Type, a.Status, 
                       a.Location, a.LocationDetail, a.Model, a.Manufacturer, a.CountryOfOrigin, 
                       a.InstallationDate, a.StartDate, a.EndDate, a.Remarks, a.CreatedDate, a.CreatedBy, 
                       a.UpdatedDate, a.UpdatedBy, a.IsDeleted, a.DeletedDate, a.DeletedBy, a.WindowsVersion,
                       (SELECT COUNT(*) FROM AssetMacAddress WHERE AssetId = a.Id) as MacAddressCount
                    FROM Asset a
                    WHERE a.IsDeleted = 0
                    ORDER BY a.CreatedDate DESC";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var assets = await connection.QueryAsync(query);

                    // ดึงข้อมูลพนักงานจากตาราง CES941
                    var employees = await GetEmployeesFromCES941();

                    // รวมข้อมูลเข้าด้วยกัน
                    var result = new List<dynamic>();
                    foreach (var asset in assets)
                    {
                        var employee = employees.FirstOrDefault(e =>
                            e.EmpNo != null && e.EmpNo.ToString() == asset.Owner
                        );
                        if (employee != null)
                        {
                            // สร้าง object ใหม่ที่มีข้อมูลจากทั้งสองตาราง
                            var combinedData = new
                            {
                                // ข้อมูลจากตาราง Asset
                                asset.Id,
                                asset.AssetNo,
                                asset.ProductSerial,
                                asset.Name,
                                asset.Type,
                                asset.Status,
                                asset.CustomerNo,
                                asset.CustomerName,
                                asset.Department,
                                asset.DepartmentName,
                                asset.Line,
                                asset.LineName,
                                asset.Owner,
                                asset.OwnerName,
                                asset.WindowsVersion,
                                asset.Location,
                                asset.LocationDetail,
                                asset.Model,
                                asset.Manufacturer,
                                asset.CountryOfOrigin,
                                asset.InstallationDate,
                                asset.StartDate,
                                asset.EndDate,
                                asset.Remarks,
                                asset.CreatedDate,
                                asset.CreatedBy,
                                asset.UpdatedDate,
                                asset.UpdatedBy,
                                asset.IsDeleted,
                                MacAddressCount = asset.MacAddressCount,

                                // ข้อมูลจากตาราง CES941
                                employee.Language,
                                employee.PrefixName,
                                employee.EmpName,
                                employee.EmpLName,
                                employee.Gender,
                                employee.EmpStartDate,
                                employee.EmpResignDate,
                                employee.LastDateAtten,
                                employee.AcceptRehired,
                                employee.ProbationDay,
                                employee.ProbationPass,
                                employee.ProbationDate,
                                employee.ContractNo,
                                employee.ContAdd,
                                employee.ContCity,
                                employee.ContZipCode,
                                employee.ContZipDesc,
                                employee.ContCountry,
                                employee.RegAdd,
                                employee.RegCity,
                                employee.RegZipCode,
                                employee.RegZipDesc,
                                employee.RegCountry,
                                employee.HomePhone,
                                employee.MobilePhone,
                                employee.Internet,
                                employee.eMailAdd,
                                employee.ADUser,
                                employee.R3User,
                                employee.DIHUser,
                                employee.LotusUser,
                                employee.SkyUser,
                                employee.RNewUser,
                                employee.BirthDate,
                                employee.BirthCity,
                                employee.BirthCountry,
                                employee.Nation,
                                employee.Nationality,
                                employee.Religion,
                                employee.BankID,
                                employee.BankAccount,
                                employee.PersonalID,
                                employee.PassPort,
                                employee.TaxID,
                                employee.SFID,
                                employee.Hospital,
                                employee.CarLicense,
                                employee.BikeLicense,
                                employee.Marital,
                                employee.MilitaryPass,
                                employee.EmpPicture,
                                employee.DocLink,
                                employee.PreEmpNo,
                                employee.ChangeDate,
                                employee.ChangeBy,
                                employee.FlagDel,
                                employee.AdminAction,
                                employee.ResignAlertDate,
                                employee.ResignKeyDate,
                                employee.Ext,
                                employee.ADPath,
                            };
                            result.Add(combinedData);
                        }
                        else
                        {
                            // ถ้าไม่พบข้อมูลพนักงาน ให้เพิ่มเฉพาะข้อมูลจากตาราง Asset
                            var assetData = new
                            {
                                asset.Id,
                                asset.AssetNo,
                                asset.ProductSerial,
                                asset.Name,
                                asset.Type,
                                asset.Status,
                                asset.CustomerNo,
                                asset.CustomerName,
                                asset.Department,
                                asset.DepartmentName,
                                asset.Line,
                                asset.LineName,
                                asset.Owner,
                                asset.OwnerName,
                                asset.WindowsVersion,
                                asset.Location,
                                asset.LocationDetail,
                                asset.Model,
                                asset.Manufacturer,
                                asset.CountryOfOrigin,
                                asset.InstallationDate,
                                asset.StartDate,
                                asset.EndDate,
                                asset.Remarks,
                                asset.CreatedDate,
                                asset.CreatedBy,
                                asset.UpdatedDate,
                                asset.UpdatedBy,
                                asset.IsDeleted,
                                MacAddressCount = asset.MacAddressCount,
                            };
                            result.Add(assetData);
                        }
                    }

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Asset asset)
        {
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    return BadRequest("ข้อมูลไม่ถูกต้อง");
                //}

                // ตรวจสอบว่า AssetNo ซ้ำหรือไม่ - จัดการกับค่า NULL
                if (!string.IsNullOrEmpty(asset.AssetNo))
                {
                    var existingAsset = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                        a.AssetNo == asset.AssetNo && !a.IsDeleted
                    );

                    if (existingAsset != null)
                    {
                        return BadRequest("รหัสทรัพย์สินนี้มีอยู่ในระบบแล้ว");
                    }
                }

                // ตรวจสอบว่า ProductSerial ซ้ำหรือไม่ - จัดการกับค่า NULL
                if (!string.IsNullOrEmpty(asset.ProductSerial))
                {
                    var existingSerial = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                        a.ProductSerial == asset.ProductSerial && !a.IsDeleted
                    );

                    if (existingSerial != null)
                    {
                        return BadRequest("Serial Number นี้มีอยู่ในระบบแล้ว");
                    }
                }

                // จัดการกับค่า NULL ก่อนบันทึก - บันทึกทุกฟิลด์
                asset.AssetNo = asset.AssetNo ?? "";
                asset.ProductSerial = asset.ProductSerial ?? "";
                asset.Name = asset.Name ?? "";
                asset.Type = asset.Type ?? "";
                asset.Status = asset.Status ?? "";
                asset.CustomerNo = asset.CustomerNo ?? "";
                asset.CustomerName = asset.CustomerName ?? "";
                asset.Department = asset.Department ?? "";
                asset.DepartmentName = asset.DepartmentName ?? "";
                asset.Line = asset.Line ?? "";
                asset.LineName = asset.LineName ?? "";
                asset.Owner = asset.Owner ?? "";
                asset.OwnerName = asset.OwnerName ?? "";
                asset.Location = asset.Location ?? "";
                asset.LocationDetail = asset.LocationDetail ?? "";
                asset.Model = asset.Model ?? "";
                asset.Manufacturer = asset.Manufacturer ?? "";
                asset.CountryOfOrigin = asset.CountryOfOrigin ?? "";
                asset.WindowsVersion = asset.WindowsVersion ?? "";
                asset.InstallationDate = asset.InstallationDate;
                asset.StartDate = asset.StartDate;
                asset.EndDate = asset.EndDate;
                asset.Remarks = asset.Remarks ?? "";

                asset.CreatedDate = DateTime.Now;
                asset.CreatedBy = GetCurrentUserId();
                asset.IsDeleted = false;

                _dbContext.Asset.Add(asset);
                await _dbContext.SaveChangesAsync();

                return Ok("เพิ่มทรัพย์สินสำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Asset asset)
        {
            try
            {
                // if (!ModelState.IsValid)
                // {
                //     return BadRequest("ข้อมูลไม่ถูกต้อง");
                // }

                var existingAsset = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                    a.Id == asset.Id && !a.IsDeleted
                );

                if (existingAsset == null)
                {
                    return NotFound("ไม่พบข้อมูลทรัพย์สิน");
                }

                // ตรวจสอบว่า AssetNo ซ้ำหรือไม่ (ยกเว้นตัวเอง) - จัดการกับค่า NULL
                if (!string.IsNullOrEmpty(asset.AssetNo))
                {
                    var duplicateAssetNo = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                        a.AssetNo == asset.AssetNo && a.Id != asset.Id && !a.IsDeleted
                    );

                    if (duplicateAssetNo != null)
                    {
                        return BadRequest("รหัสทรัพย์สินนี้มีอยู่ในระบบแล้ว");
                    }
                }

                // ตรวจสอบว่า ProductSerial ซ้ำหรือไม่ (ยกเว้นตัวเอง) - จัดการกับค่า NULL
                if (!string.IsNullOrEmpty(asset.ProductSerial))
                {
                    var duplicateSerial = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                        a.ProductSerial == asset.ProductSerial && a.Id != asset.Id && !a.IsDeleted
                    );

                    if (duplicateSerial != null)
                    {
                        return BadRequest("Serial Number นี้มีอยู่ในระบบแล้ว");
                    }
                }

                // อัปเดตข้อมูล - จัดการกับค่า NULL และอัปเดตทุกฟิลด์
                existingAsset.AssetNo = asset.AssetNo ?? "";
                existingAsset.ProductSerial = asset.ProductSerial ?? "";
                existingAsset.Name = asset.Name ?? "";
                existingAsset.Type = asset.Type ?? "";
                existingAsset.Status = asset.Status ?? "";
                existingAsset.CustomerNo = asset.CustomerNo ?? "";
                existingAsset.CustomerName = asset.CustomerName ?? "";
                existingAsset.Department = asset.Department ?? "";
                existingAsset.DepartmentName = asset.DepartmentName ?? "";
                existingAsset.Line = asset.Line ?? "";
                existingAsset.LineName = asset.LineName ?? "";
                existingAsset.Owner = asset.Owner ?? "";
                existingAsset.OwnerName = asset.OwnerName ?? "";
                existingAsset.Location = asset.Location ?? "";
                existingAsset.LocationDetail = asset.LocationDetail ?? "";
                existingAsset.Model = asset.Model ?? "";
                existingAsset.Manufacturer = asset.Manufacturer ?? "";
                existingAsset.CountryOfOrigin = asset.CountryOfOrigin ?? "";
                existingAsset.WindowsVersion = asset.WindowsVersion ?? "";
                existingAsset.InstallationDate = asset.InstallationDate;
                existingAsset.StartDate = asset.StartDate;
                existingAsset.EndDate = asset.EndDate;
                existingAsset.Remarks = asset.Remarks ?? "";
                existingAsset.UpdatedDate = DateTime.Now;
                existingAsset.UpdatedBy = GetCurrentUserId();

                await _dbContext.SaveChangesAsync();

                return Ok("แก้ไขทรัพย์สินสำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var asset = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                    a.Id == id && !a.IsDeleted
                );

                if (asset == null)
                {
                    return NotFound("ไม่พบข้อมูลทรัพย์สิน");
                }

                // Soft delete
                asset.IsDeleted = true;
                asset.DeletedDate = DateTime.Now;
                asset.DeletedBy = GetCurrentUserId();

                await _dbContext.SaveChangesAsync();

                return Ok("ลบทรัพย์สินสำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        // เพิ่มเมธอดสำหรับดึงข้อมูลพนักงานจาก CES941
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
                    var data = await db.QueryAsync(sql);
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting employees from CES941: {ex.Message}");
                return new List<dynamic>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var asset = await _dbContext
                    .Asset.AsNoTracking()
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .Select(a => new
                    {
                        a.Id,
                        AssetNo = a.AssetNo ?? "",
                        ProductSerial = a.ProductSerial ?? "",
                        Name = a.Name ?? "",
                        Type = a.Type ?? "",
                        Status = a.Status ?? "",
                        CustomerNo = a.CustomerNo ?? "",
                        CustomerName = a.CustomerName ?? "",
                        Department = a.Department ?? "",
                        DepartmentName = a.DepartmentName ?? "",
                        Line = a.Line ?? "",
                        LineName = a.LineName ?? "",
                        Owner = a.Owner ?? "",
                        OwnerName = a.OwnerName ?? "",
                        Location = a.Location ?? "",
                        LocationDetail = a.LocationDetail ?? "",
                        Model = a.Model ?? "",
                        Manufacturer = a.Manufacturer ?? "",
                        CountryOfOrigin = a.CountryOfOrigin ?? "",
                        WindowsVersion = a.WindowsVersion ?? "",
                        InstallationDate = a.InstallationDate,
                        StartDate = a.StartDate,
                        EndDate = a.EndDate,
                        Remarks = a.Remarks ?? "",
                        CreatedDate = a.CreatedDate,
                        CreatedBy = a.CreatedBy ?? "",
                        UpdatedDate = a.UpdatedDate,
                        UpdatedBy = a.UpdatedBy ?? "",
                        IsDeleted = a.IsDeleted,
                        DeletedDate = a.DeletedDate,
                        DeletedBy = a.DeletedBy ?? "",
                    })
                    .FirstOrDefaultAsync();

                if (asset == null)
                    return NotFound("ไม่พบข้อมูลทรัพย์สิน");

                return Json(asset);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var query =
                    @"
                    SELECT
                        COUNT(*) as Total,
                        SUM(CASE WHEN Status = '1' THEN 1 ELSE 0 END) as Active,
                        SUM(CASE WHEN Status = '2' THEN 1 ELSE 0 END) as Repair,
                        SUM(CASE WHEN Status = '3' THEN 1 ELSE 0 END) as Damaged,
                        SUM(CASE WHEN Status = '4' THEN 1 ELSE 0 END) as Disposed,
                        SUM(CASE WHEN Status = '5' THEN 1 ELSE 0 END) as Inactive
                    FROM Asset
                    WHERE IsDeleted = 0";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var stats = await connection.QueryFirstAsync(query);
                    return Json(stats);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await GetEmployeesFromCES941();
                return Json(employees);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPersons(string q)
        {
            try
            {
                using (var cesConn = new SqlConnection(_ces941ConnectionString))
                using (var mainConn = new SqlConnection(_connectionString))
                {
                    // ดึงรายชื่อพนักงานจาก MEmpBasic
                    var sqlEmp =
                        @"SELECT EmpNo, EmpName, EmpLName FROM MEmpBasic WHERE Language = 'EN' AND EmpResignDate IS NULL";
                    var persons = (await cesConn.QueryAsync(sqlEmp)).ToList();

                    // แปลง EmpNo, EmpName, EmpLName ให้เป็น string เพื่อป้องกันปัญหา type
                    var personList = persons
                        .Select(p => new
                        {
                            EmpNo = p.EmpNo != null ? p.EmpNo.ToString() : "",
                            EmpName = p.EmpName != null ? p.EmpName.ToString() : "",
                            EmpLName = p.EmpLName != null ? p.EmpLName.ToString() : "",
                        })
                        .ToList();

                    // ถ้ามี q ให้ filter
                    if (!string.IsNullOrEmpty(q))
                    {
                        personList = personList
                            .Where(p =>
                                (
                                    !string.IsNullOrEmpty(p.EmpNo)
                                    && p.EmpNo.Contains(q, StringComparison.OrdinalIgnoreCase)
                                )
                                || (
                                    !string.IsNullOrEmpty(p.EmpName)
                                    && p.EmpName.Contains(q, StringComparison.OrdinalIgnoreCase)
                                )
                                || (
                                    !string.IsNullOrEmpty(p.EmpLName)
                                    && p.EmpLName.Contains(q, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            .ToList();
                    }

                    // ดึงแผนกล่าสุดของแต่ละ EmpNo จาก emp_person_shift
                    var empNos = personList
                        .Select(p => p.EmpNo)
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();
                    if (empNos.Count == 0)
                        return Json(new List<object>());

                    var empNoTable = string.Join(
                        ",",
                        empNos.Select(e => "'" + e.Replace("'", "''") + "'")
                    );
                    var sqlDept =
                        $@"
                        SELECT t1.personID, t1.deptName
                        FROM emp_person_shift t1
                        INNER JOIN (
                            SELECT personID, MAX(date) AS MaxDate
                            FROM emp_person_shift
                            WHERE personID IN ({empNoTable})
                            GROUP BY personID
                        ) t2 ON t1.personID = t2.personID AND t1.date = t2.MaxDate
                    ";
                    var deptList = (await mainConn.QueryAsync(sqlDept)).ToList();

                    // แปลง personID, deptName ให้เป็น string เช่นกัน
                    var deptListString = deptList
                        .Select(d => new
                        {
                            personID = d.personID != null ? d.personID.ToString() : "",
                            deptName = d.deptName != null ? d.deptName.ToString() : "",
                        })
                        .ToList();

                    var result = personList.Select(p =>
                    {
                        var dept = deptListString.FirstOrDefault(d => d.personID == p.EmpNo);
                        return new
                        {
                            value = p.EmpNo,
                            text = p.EmpName
                                + " "
                                + p.EmpLName
                                + (
                                    dept != null && !string.IsNullOrEmpty(dept.deptName)
                                        ? " (" + dept.deptName + ")"
                                        : ""
                                ),
                        };
                    });
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var query =
                    @"
                    SELECT TOP (200) 
                        rowAutoID, code, parentCode, deptID, name, pinyin, deptLevel, inID, inName, director, phone, fax, address, zip, subSystem, note, reserve1, reserve2, reserve3, reserve4, modifyTime, operator, groupID, timeGroup, 
                        cardCategory, cardStatus, ParkMax1, ParkMax2, ParkNow1, ParkNow2, HomeSize, Credit, etag, email, Emergency_Contacts, Emergency_TEL, ManageFee
                    FROM Dept
                ";

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var departments = await connection.QueryAsync(query);
                    return Json(departments);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetTypes()
        {
            try
            {
                var types = new[]
                {
                    new
                    {
                        Id = AssetType.Computer,
                        Text = AssetType.GetDisplayName(AssetType.Computer),
                    },
                    new
                    {
                        Id = AssetType.Laptop,
                        Text = AssetType.GetDisplayName(AssetType.Laptop),
                    },
                    new
                    {
                        Id = AssetType.Tablet,
                        Text = AssetType.GetDisplayName(AssetType.Tablet),
                    },
                    new { Id = AssetType.Phone, Text = AssetType.GetDisplayName(AssetType.Phone) },
                    new
                    {
                        Id = AssetType.Printer,
                        Text = AssetType.GetDisplayName(AssetType.Printer),
                    },
                    new
                    {
                        Id = AssetType.NetworkDevice,
                        Text = AssetType.GetDisplayName(AssetType.NetworkDevice),
                    },
                    new
                    {
                        Id = AssetType.Machine,
                        Text = AssetType.GetDisplayName(AssetType.Machine),
                    },
                    new { Id = AssetType.Tool, Text = AssetType.GetDisplayName(AssetType.Tool) },
                    new
                    {
                        Id = AssetType.Equipment,
                        Text = AssetType.GetDisplayName(AssetType.Equipment),
                    },
                    new { Id = AssetType.Other, Text = AssetType.GetDisplayName(AssetType.Other) },
                };

                return Json(types);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetStatuses()
        {
            try
            {
                var statuses = new[]
                {
                    new
                    {
                        Id = AssetStatus.Active,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Active),
                    },
                    new
                    {
                        Id = AssetStatus.Repair,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Repair),
                    },
                    new
                    {
                        Id = AssetStatus.Damaged,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Damaged),
                    },
                    new
                    {
                        Id = AssetStatus.Disposed,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Disposed),
                    },
                    new
                    {
                        Id = AssetStatus.Inactive,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Inactive),
                    },
                    new
                    {
                        Id = AssetStatus.Spare,
                        Text = AssetStatus.GetDisplayName(AssetStatus.Spare),
                    },
                };
                return Json(statuses);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetLocations()
        {
            try
            {
                var locations = new[]
                {
                    new
                    {
                        Id = AssetLocation.ProductionArea,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ProductionArea),
                    },
                    new
                    {
                        Id = AssetLocation.RawMaterialWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.RawMaterialWH),
                    },
                    new
                    {
                        Id = AssetLocation.ElectronicWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ElectronicWH),
                    },
                    new
                    {
                        Id = AssetLocation.SemiFinishedWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SemiFinishedWH),
                    },
                    new
                    {
                        Id = AssetLocation.DQELab,
                        Text = AssetLocation.GetDisplayName(AssetLocation.DQELab),
                    },
                    new
                    {
                        Id = AssetLocation.IQCRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.IQCRoom),
                    },
                    new
                    {
                        Id = AssetLocation.StoreArea,
                        Text = AssetLocation.GetDisplayName(AssetLocation.StoreArea),
                    },
                    new
                    {
                        Id = AssetLocation.SparePartsRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SparePartsRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ServerRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ServerRoom),
                    },
                    new
                    {
                        Id = AssetLocation.Office,
                        Text = AssetLocation.GetDisplayName(AssetLocation.Office),
                    },
                    new
                    {
                        Id = AssetLocation.BossRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.BossRoom),
                    },
                    new
                    {
                        Id = AssetLocation.MeetingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.MeetingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.TrainingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.TrainingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ConferenceRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ConferenceRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ProductionOffice,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ProductionOffice),
                    },
                    new
                    {
                        Id = AssetLocation.PrintingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.PrintingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ColorMixingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ColorMixingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ScreenRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ScreenRoom),
                    },
                    new
                    {
                        Id = AssetLocation.SecurityRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SecurityRoom),
                    },
                    new
                    {
                        Id = AssetLocation.StaffDormitory,
                        Text = AssetLocation.GetDisplayName(AssetLocation.StaffDormitory),
                    },
                    new
                    {
                        Id = AssetLocation.ServerRoom2,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ServerRoom2),
                    },
                    new
                    {
                        Id = AssetLocation.ChangingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ChangingRoom),
                    },
                };

                return Json(locations);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                var locations = new[]
                {
                    new
                    {
                        Id = AssetLocation.ProductionArea,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ProductionArea),
                    },
                    new
                    {
                        Id = AssetLocation.RawMaterialWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.RawMaterialWH),
                    },
                    new
                    {
                        Id = AssetLocation.ElectronicWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ElectronicWH),
                    },
                    new
                    {
                        Id = AssetLocation.SemiFinishedWH,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SemiFinishedWH),
                    },
                    new
                    {
                        Id = AssetLocation.DQELab,
                        Text = AssetLocation.GetDisplayName(AssetLocation.DQELab),
                    },
                    new
                    {
                        Id = AssetLocation.IQCRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.IQCRoom),
                    },
                    new
                    {
                        Id = AssetLocation.StoreArea,
                        Text = AssetLocation.GetDisplayName(AssetLocation.StoreArea),
                    },
                    new
                    {
                        Id = AssetLocation.SparePartsRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SparePartsRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ServerRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ServerRoom),
                    },
                    new
                    {
                        Id = AssetLocation.Office,
                        Text = AssetLocation.GetDisplayName(AssetLocation.Office),
                    },
                    new
                    {
                        Id = AssetLocation.BossRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.BossRoom),
                    },
                    new
                    {
                        Id = AssetLocation.MeetingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.MeetingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.TrainingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.TrainingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ConferenceRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ConferenceRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ProductionOffice,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ProductionOffice),
                    },
                    new
                    {
                        Id = AssetLocation.PrintingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.PrintingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ColorMixingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ColorMixingRoom),
                    },
                    new
                    {
                        Id = AssetLocation.ScreenRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ScreenRoom),
                    },
                    new
                    {
                        Id = AssetLocation.SecurityRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.SecurityRoom),
                    },
                    new
                    {
                        Id = AssetLocation.StaffDormitory,
                        Text = AssetLocation.GetDisplayName(AssetLocation.StaffDormitory),
                    },
                    new
                    {
                        Id = AssetLocation.ServerRoom2,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ServerRoom2),
                    },
                    new
                    {
                        Id = AssetLocation.ChangingRoom,
                        Text = AssetLocation.GetDisplayName(AssetLocation.ChangingRoom),
                    },
                };

                return Json(locations);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonName(string empNo)
        {
            if (string.IsNullOrEmpty(empNo))
                return Json(new { name = "", deptName = "" });
            try
            {
                // ถ้า emp_person_shift หาไม่เจอ ให้หาจาก ces941
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql =
                        @"SELECT TOP 1 name, deptName FROM emp_person_shift WHERE personID = @empNo ORDER BY date DESC";
                    var result = await conn.QueryFirstOrDefaultAsync(sql, new { empNo });

                    if (result != null)
                    {
                        return Json(
                            new { name = result?.name ?? "", deptName = result?.deptName ?? "" }
                        );
                    }
                    else
                    {
                        // หาใน ces941 ถ้า emp_person_shift ไม่เจอ
                        // ดึงข้อมูลจากฐานข้อมูล CES941 ถ้าไม่พบใน emp_person_shift
                        using (var ces941 = new SqlConnection(_ces941ConnectionString))
                        {
                            var sqlCes =
                                @"SELECT TOP 1 PrefixName, EmpName as name, EmpLName as lastName FROM MEmpBasic WHERE EmpNo = @empNo AND Language = 'EN'";
                            var resultCes = await ces941.QueryFirstOrDefaultAsync(
                                sqlCes,
                                new { empNo }
                            );

                            // รวมชื่อ-นามสกุล (ภาษาไทย)
                            string fullName = "";
                            if (resultCes != null)
                            {
                                if (!string.IsNullOrEmpty(resultCes?.PrefixName))
                                    fullName += resultCes.PrefixName + " ";
                                if (!string.IsNullOrEmpty(resultCes?.name))
                                    fullName += resultCes.name + " ";
                                if (!string.IsNullOrEmpty(resultCes?.lastName))
                                    fullName += resultCes.lastName;
                                fullName = fullName.Trim();
                            }

                            return Json(new { name = fullName, deptName = "" });
                        }
                    }
                }
            }
            catch
            {
                return Json(new { name = "", deptName = "" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLines()
        {
            try
            {
                using (var connection = new OracleConnection(_oracleConnectionString))
                {
                    await connection.OpenAsync();
                    var query =
                        @"
                        SELECT 
                            PDLINE_ID as Id,
                            PDLINE_NAME as Text
                        FROM sys_pdline 
                        WHERE ENABLED = 'Y' 
                        AND PDLINE_DESC2 IN ('BL','CHAS')
                        ORDER BY PDLINE_NAME";

                    using (var command = new OracleCommand(query, connection))
                    {
                        var result = new List<dynamic>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(
                                    new
                                    {
                                        Id = reader["Id"] == DBNull.Value
                                            ? null
                                            : reader["Id"].ToString(),
                                        Text = reader["Text"] == DBNull.Value
                                            ? null
                                            : reader["Text"].ToString(),
                                    }
                                );
                            }
                        }
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWindowsVersions()
        {
            try
            {
                var versions = WindowsVersion.GetVersions().Select(v => new { Id = v, Text = v });
                return Json(versions);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("Asset/GetAssetMacAddresses")]
        public async Task<IActionResult> GetAssetMacAddresses(int assetId)
        {
            try
            {
                var macAddresses = await _dbContext
                    .AssetMacAddress.Where(m => m.AssetId == assetId)
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                return Json(macAddresses);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMacAddresses(int assetId)
        {
            try
            {
                var macAddresses = await _dbContext
                    .AssetMacAddress.Where(m => m.AssetId == assetId)
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                return Json(macAddresses);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddMacAddress([FromBody] AssetMacAddress macAddress)
        {
            try
            {
                // ตรวจสอบข้อมูลที่จำเป็น
                if (macAddress == null)
                {
                    return BadRequest("ข้อมูล Mac Address ไม่ถูกต้อง");
                }

                if (macAddress.AssetId <= 0)
                {
                    return BadRequest("ไม่พบข้อมูลทรัพย์สิน");
                }

                if (string.IsNullOrWhiteSpace(macAddress.MacAddress))
                {
                    return BadRequest("กรุณากรอก Mac Address");
                }

                // ตรวจสอบรูปแบบ Mac Address
                if (!IsValidMacAddress(macAddress.MacAddress))
                {
                    return BadRequest(
                        "รูปแบบ Mac Address ไม่ถูกต้อง กรุณาใช้รูปแบบ XX:XX:XX:XX:XX:XX หรือ XX-XX-XX-XX-XX-XX"
                    );
                }

                // ตรวจสอบ reserved Mac Address
                if (IsReservedMacAddress(macAddress.MacAddress))
                {
                    return BadRequest("ไม่สามารถใช้ Mac Address นี้ได้ (เป็น reserved address)");
                }

                // ตรวจสอบว่า Asset มีอยู่จริง
                var asset = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                    a.Id == macAddress.AssetId && !a.IsDeleted
                );
                if (asset == null)
                {
                    return BadRequest("ไม่พบข้อมูลทรัพย์สิน");
                }

                // ตรวจสอบ Mac Address ซ้ำในทรัพย์สินเดียวกัน
                var duplicateInSameAsset = await _dbContext.AssetMacAddress.FirstOrDefaultAsync(m =>
                    m.AssetId == macAddress.AssetId && m.MacAddress == macAddress.MacAddress
                );

                if (duplicateInSameAsset != null)
                {
                    return BadRequest("Mac Address นี้มีอยู่ในทรัพย์สินนี้แล้ว");
                }

                // ตรวจสอบจำนวน Mac Address ที่มีอยู่ในทรัพย์สินนี้ (จำกัดไม่เกิน 10 รายการ)
                var macAddressCount = await _dbContext.AssetMacAddress.CountAsync(m =>
                    m.AssetId == macAddress.AssetId
                );
                if (macAddressCount >= 10)
                {
                    return BadRequest(
                        "ทรัพย์สินนี้มี Mac Address ครบ 10 รายการแล้ว ไม่สามารถเพิ่มได้อีก"
                    );
                }

                // ตรวจสอบ Mac Address ซ้ำในระบบทั้งหมด
                var existingMac = await _dbContext.AssetMacAddress.FirstOrDefaultAsync(m =>
                    m.MacAddress == macAddress.MacAddress
                );

                if (existingMac != null)
                {
                    if (existingMac.AssetId == macAddress.AssetId)
                    {
                        return BadRequest("Mac Address นี้มีอยู่ในทรัพย์สินนี้แล้ว");
                    }
                    else
                    {
                        return BadRequest(
                            $"Mac Address นี้มีอยู่ในทรัพย์สินอื่นแล้ว (Asset ID: {existingMac.AssetId})"
                        );
                    }
                }

                // จัดการกับค่า NULL และแปลง Mac Address เป็นรูปแบบมาตรฐาน
                macAddress.MacAddress = NormalizeMacAddress(macAddress.MacAddress?.Trim());
                macAddress.NetworkType = macAddress.NetworkType ?? "Ethernet";
                macAddress.Remarks = macAddress.Remarks ?? "";
                macAddress.CreatedDate = DateTime.Now;
                macAddress.CreatedBy = GetCurrentUserId();

                _dbContext.AssetMacAddress.Add(macAddress);
                await _dbContext.SaveChangesAsync();

                // บันทึกประวัติ
                await LogAssetHistory(
                    macAddress.AssetId,
                    "AddMacAddress",
                    "MacAddress",
                    null,
                    macAddress.MacAddress,
                    $"เพิ่ม Mac Address: {macAddress.MacAddress}"
                );

                // ส่งข้อมูลกลับเพื่อแสดงในหน้าเว็บ
                var result = new
                {
                    success = true,
                    message = "เพิ่ม Mac Address สำเร็จ",
                    data = new
                    {
                        id = macAddress.Id,
                        assetId = macAddress.AssetId,
                        macAddress = macAddress.MacAddress,
                        networkType = macAddress.NetworkType,
                        remarks = macAddress.Remarks,
                        createdDate = macAddress.CreatedDate,
                        createdBy = macAddress.CreatedBy,
                    },
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMacAddress(int id)
        {
            try
            {
                var macAddress = await _dbContext.AssetMacAddress.FindAsync(id);
                if (macAddress == null)
                {
                    return NotFound("ไม่พบ Mac Address");
                }

                var assetId = macAddress.AssetId;
                var macAddressValue = macAddress.MacAddress;

                _dbContext.AssetMacAddress.Remove(macAddress);
                await _dbContext.SaveChangesAsync();

                // บันทึกประวัติ
                await LogAssetHistory(
                    assetId,
                    "DeleteMacAddress",
                    "MacAddress",
                    macAddressValue,
                    null,
                    $"ลบ Mac Address: {macAddressValue}"
                );

                return Ok("ลบ Mac Address สำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetHistory(int assetId)
        {
            try
            {
                var history = await _dbContext
                    .AssetHistory.Where(h => h.AssetId == assetId)
                    .OrderByDescending(h => h.ChangeDate)
                    .ToListAsync();

                return Json(history);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private async Task LogAssetHistory(
            int assetId,
            string changeType,
            string fieldName = null,
            string oldValue = null,
            string newValue = null,
            string description = null
        )
        {
            try
            {
                var history = new AssetHistory
                {
                    AssetId = assetId,
                    ChangeType = changeType,
                    FieldName = fieldName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Description = description,
                    ChangeDate = DateTime.Now,
                    ChangedBy = GetCurrentUserId(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                };

                _dbContext.AssetHistory.Add(history);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main operation
                Console.WriteLine($"Error logging asset history: {ex.Message}");
            }
        }

        private string GetCurrentUserId()
        {
            // ดึง User ID จาก Session หรือ Claims
            return HttpContext.Session.GetString("UserId") ?? "System";
        }

        /// <summary>
        /// ตรวจสอบรูปแบบ Mac Address ที่ถูกต้อง
        /// </summary>
        private bool IsValidMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return false;

            // รูปแบบ Mac Address ที่ยอมรับ: XX:XX:XX:XX:XX:XX หรือ XX-XX-XX-XX-XX-XX
            var pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
            return System.Text.RegularExpressions.Regex.IsMatch(macAddress, pattern);
        }

        /// <summary>
        /// แปลง Mac Address เป็นรูปแบบมาตรฐาน (XX:XX:XX:XX:XX:XX)
        /// </summary>
        private string NormalizeMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return macAddress;

            // ลบเครื่องหมาย - และ : ออก
            var cleanMac = macAddress.Replace("-", "").Replace(":", "");

            // ตรวจสอบความยาว
            if (cleanMac.Length != 12)
                return macAddress;

            // แปลงเป็นรูปแบบ XX:XX:XX:XX:XX:XX
            return $"{cleanMac.Substring(0, 2)}:{cleanMac.Substring(2, 2)}:{cleanMac.Substring(4, 2)}:{cleanMac.Substring(6, 2)}:{cleanMac.Substring(8, 2)}:{cleanMac.Substring(10, 2)}";
        }

        /// <summary>
        /// ตรวจสอบ Mac Address ที่เป็น reserved หรือ special addresses
        /// </summary>
        private bool IsReservedMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return false;

            var normalizedMac = NormalizeMacAddress(macAddress).ToUpper();

            // ตรวจสอบ Multicast addresses (01:00:5E:xx:xx:xx)
            if (normalizedMac.StartsWith("01:00:5E:"))
                return true;

            // ตรวจสอบ Broadcast address (FF:FF:FF:FF:FF:FF)
            if (normalizedMac == "FF:FF:FF:FF:FF:FF")
                return true;

            // ตรวจสอบ Locally administered addresses (02:xx:xx:xx:xx:xx, 06:xx:xx:xx:xx:xx, 0A:xx:xx:xx:xx:xx, 0E:xx:xx:xx:xx:xx)
            var firstByte = normalizedMac.Substring(0, 2);
            if (firstByte == "02" || firstByte == "06" || firstByte == "0A" || firstByte == "0E")
                return true;

            return false;
        }

        // ฟังก์ชันสำหรับทำซ้ำข้อมูลทรัพย์สิน (Duplicate)
        [HttpGet]
        public async Task<IActionResult> Duplicate(int id)
        {
            try
            {
                var asset = await _dbContext
                    .Asset.AsNoTracking()
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (asset == null)
                    return NotFound("ไม่พบข้อมูลทรัพย์สิน");

                // คืนค่า Asset โดยไม่คืนค่า Id, AssetNo, Name (ให้เป็นค่าว่าง)
                var duplicate = new
                {
                    Id = (int?)null,
                    AssetNo = "",
                    ProductSerial = asset.ProductSerial ?? "",
                    Name = "",
                    Type = asset.Type ?? "",
                    Status = asset.Status ?? "",
                    CustomerNo = asset.CustomerNo ?? "",
                    CustomerName = asset.CustomerName ?? "",
                    Department = asset.Department ?? "",
                    DepartmentName = asset.DepartmentName ?? "",
                    Line = asset.Line ?? "",
                    LineName = asset.LineName ?? "",
                    Owner = asset.Owner ?? "",
                    OwnerName = asset.OwnerName ?? "",
                    Location = asset.Location ?? "",
                    LocationDetail = asset.LocationDetail ?? "",
                    Model = asset.Model ?? "",
                    Manufacturer = asset.Manufacturer ?? "",
                    CountryOfOrigin = asset.CountryOfOrigin ?? "",
                    WindowsVersion = asset.WindowsVersion ?? "",
                    InstallationDate = asset.InstallationDate,
                    StartDate = asset.StartDate,
                    EndDate = asset.EndDate,
                    Remarks = asset.Remarks ?? "",
                };
                return Json(duplicate);
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditMacAddress([FromBody] AssetMacAddress model)
        {
            try
            {
                if (model == null || model.Id <= 0)
                    return BadRequest("ข้อมูลไม่ถูกต้อง");

                var mac = await _dbContext.AssetMacAddress.FirstOrDefaultAsync(m =>
                    m.Id == model.Id
                );
                if (mac == null)
                    return NotFound("ไม่พบ Mac Address");

                // ตรวจสอบรูปแบบ
                if (!IsValidMacAddress(model.MacAddress))
                    return BadRequest(
                        "รูปแบบ Mac Address ไม่ถูกต้อง กรุณาใช้ XX:XX:XX:XX:XX:XX หรือ XX-XX-XX-XX-XX-XX"
                    );

                // Normalize
                var normalized = NormalizeMacAddress(model.MacAddress?.Trim());

                // ตรวจสอบซ้ำในทรัพย์สินเดียวกัน (ยกเว้นตัวเอง)
                var duplicate = await _dbContext.AssetMacAddress.FirstOrDefaultAsync(m =>
                    m.AssetId == mac.AssetId && m.MacAddress == normalized && m.Id != mac.Id
                );
                if (duplicate != null)
                    return BadRequest("Mac Address นี้มีอยู่ในทรัพย์สินนี้แล้ว");

                // ตรวจสอบซ้ำในระบบ (ยกเว้นตัวเอง)
                var exist = await _dbContext.AssetMacAddress.FirstOrDefaultAsync(m =>
                    m.MacAddress == normalized && m.Id != mac.Id
                );
                if (exist != null)
                    return BadRequest(
                        $"Mac Address นี้มีอยู่ในทรัพย์สินอื่นแล้ว (Asset ID: {exist.AssetId})"
                    );

                // อัปเดต
                var oldMac = mac.MacAddress;
                mac.MacAddress = normalized;
                await _dbContext.SaveChangesAsync();

                // Log ประวัติ
                await LogAssetHistory(
                    mac.AssetId,
                    "EditMacAddress",
                    "MacAddress",
                    oldMac,
                    normalized,
                    $"แก้ไข Mac Address: {oldMac} → {normalized}"
                );

                return Ok("แก้ไข Mac Address สำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeStatusModel model)
        {
            try
            {
                if (model == null || model.Id <= 0)
                {
                    return BadRequest("ข้อมูลไม่ถูกต้อง");
                }

                var asset = await _dbContext.Asset.FirstOrDefaultAsync(a =>
                    a.Id == model.Id && !a.IsDeleted
                );

                if (asset == null)
                {
                    return NotFound("ไม่พบข้อมูลทรัพย์สิน");
                }

                // ตรวจสอบสถานะที่ถูกต้อง
                var validStatuses = new[] { "1", "2", "3", "4", "5", "6" };
                if (!validStatuses.Contains(model.Status))
                {
                    return BadRequest("สถานะไม่ถูกต้อง");
                }

                var oldStatus = asset.Status;
                var oldStatusName = AssetStatus.GetDisplayName(oldStatus);
                var newStatusName = AssetStatus.GetDisplayName(model.Status);

                // อัปเดตสถานะ
                asset.Status = model.Status;
                asset.UpdatedDate = DateTime.Now;
                asset.UpdatedBy = GetCurrentUserId();

                await _dbContext.SaveChangesAsync();

                // บันทึกประวัติการเปลี่ยนแปลง
                await LogAssetHistory(
                    asset.Id,
                    "ChangeStatus",
                    "Status",
                    oldStatus,
                    model.Status,
                    $"เปลี่ยนสถานะ: {oldStatusName} → {newStatusName}"
                );

                return Ok($"เปลี่ยนสถานะเป็น {newStatusName} สำเร็จ");
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // ดึงข้อมูลสรุปทรัพย์สิน
                    var summaryQuery =
                        @"
                        SELECT
                            COUNT(*) as Total,
                            SUM(CASE WHEN Status = '1' THEN 1 ELSE 0 END) as Active,
                            SUM(CASE WHEN Status = '2' THEN 1 ELSE 0 END) as Repair,
                            SUM(CASE WHEN Status = '3' THEN 1 ELSE 0 END) as Damaged,
                            SUM(CASE WHEN Status = '4' THEN 1 ELSE 0 END) as Disposed,
                            SUM(CASE WHEN Status = '5' THEN 1 ELSE 0 END) as Inactive,
                            SUM(CASE WHEN Status = '6' THEN 1 ELSE 0 END) as Spare
                        FROM Asset
                        WHERE IsDeleted = 0";

                    var summary = await connection.QueryFirstAsync(summaryQuery);

                    // ดึงข้อมูลจำนวน Mac Address
                    var macQuery =
                        @"
                        SELECT 
                            COUNT(DISTINCT a.Id) as WithMac,
                            (SELECT COUNT(*) FROM Asset WHERE IsDeleted = 0) - COUNT(DISTINCT a.Id) as WithoutMac
                        FROM Asset a
                        INNER JOIN AssetMacAddress am ON a.Id = am.AssetId
                        WHERE a.IsDeleted = 0";

                    var macStats = await connection.QueryFirstAsync(macQuery);

                    // ดึงข้อมูลสรุปตามประเภท
                    var typeQuery =
                        @"
                        SELECT 
                            Type,
                            COUNT(*) as Count
                        FROM Asset
                        WHERE IsDeleted = 0
                        GROUP BY Type
                        ORDER BY Count DESC";

                    var typeStats = await connection.QueryAsync(typeQuery);
                    var byType = new Dictionary<string, int>();
                    foreach (var item in typeStats)
                    {
                        byType[item.Type] = item.Count;
                    }

                    // ดึงข้อมูลสรุปตามสถานที่
                    var locationQuery =
                        @"
                        SELECT 
                            Location,
                            COUNT(*) as Count
                        FROM Asset
                        WHERE IsDeleted = 0
                        GROUP BY Location
                        ORDER BY Count DESC";

                    var locationStats = await connection.QueryAsync(locationQuery);
                    var byLocation = new Dictionary<string, int>();
                    foreach (var item in locationStats)
                    {
                        byLocation[item.Location] = item.Count;
                    }

                    // ดึงข้อมูลทรัพย์สินที่เพิ่งเพิ่มในเดือนนี้
                    var recentQuery =
                        @"
                        SELECT COUNT(*) as RecentCount
                        FROM Asset
                        WHERE IsDeleted = 0 
                        AND CreatedDate >= DATEADD(MONTH, -1, GETDATE())";

                    var recentCount = await connection.QueryFirstAsync<int>(recentQuery);

                    // ดึงข้อมูลทรัพย์สินที่ต้องบำรุงรักษา (สถานะกำลังซ่อม)
                    var maintenanceQuery =
                        @"
                        SELECT COUNT(*) as MaintenanceCount
                        FROM Asset
                        WHERE IsDeleted = 0 AND Status = '2'";

                    var maintenanceCount = await connection.QueryFirstAsync<int>(maintenanceQuery);

                    var result = new
                    {
                        total = summary.Total,
                        active = summary.Active,
                        repair = summary.Repair,
                        damaged = summary.Damaged,
                        disposed = summary.Disposed,
                        inactive = summary.Inactive,
                        spare = summary.Spare,
                        withMac = macStats.WithMac,
                        withoutMac = macStats.WithoutMac,
                        byType = byType,
                        byLocation = byLocation,
                        recentCount = recentCount,
                        maintenanceCount = maintenanceCount,
                    };

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("Asset/GetByOwner")]
        public async Task<IActionResult> GetByOwner(string owner)
        {
            try
            {
                var query =
                    @"
                    SELECT 
                        Id, 
                        AssetNo, 
                        ProductSerial, 
                        Name, 
                        Type, 
                        Model, 
                        Status,
                        Location,
                        LocationDetail,
                        Manufacturer,
                        WindowsVersion,
                        InstallationDate,
                        StartDate,
                        EndDate,
                        Remarks,
                        CreatedDate,
                        UpdatedDate,
                        (SELECT COUNT(*) FROM AssetMacAddress WHERE AssetId = Asset.Id) as MacAddressCount
                    FROM Asset 
                    WHERE Owner = @Owner AND IsDeleted = 0 
                    ORDER BY CreatedDate DESC";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var assets = await connection.QueryAsync(query, new { Owner = owner });
                    foreach (var item in assets)
                    {
                        item.Type = AssetType.GetDisplayName(item.Type);
                    }
                    return Json(assets);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }
    }
}
