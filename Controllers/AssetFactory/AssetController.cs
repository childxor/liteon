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
using Newtonsoft.Json;

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

        public AssetController(IConfiguration configuration, ApplicationDbContext context)
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
                LoadPermissions("Asset", "Index");
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
                // ดึงข้อมูลจากตาราง Asset
                var query =
                    @"
                    SELECT  
                        Id, Code, Name, Type, Status, OwnerId, OwnerName, 
                        Department, DepartmentName, Location, ProductionLine, 
                        PurchaseDate, Warranty, Description, CreatedDate, 
                        CreatedBy, UpdatedDate, UpdatedBy, IsDeleted
                    FROM Asset
                    WHERE IsDeleted = 0";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var assets = await connection.QueryAsync(query);

                    // ดึงข้อมูลพนักงานจากตาราง CES941
                    var employees = await GetEmployeesFromCES941();

                    // รวมข้อมูลเข้าด้วยกัน
                    var result = new List<dynamic>();
                    foreach (var asset in assets)
                    {
                        var employee = employees.FirstOrDefault(e => e.EmpNo == asset.OwnerId);
                        if (employee != null)
                        {
                            // สร้าง object ใหม่ที่มีข้อมูลจากทั้งสองตาราง
                            var combinedData = new
                            {
                                // ข้อมูลจากตาราง Asset
                                asset.Id,
                                asset.Code,
                                asset.Name,
                                asset.Type,
                                asset.Status,
                                asset.OwnerId,
                                asset.OwnerName,
                                asset.Department,
                                asset.DepartmentName,
                                asset.Location,
                                asset.ProductionLine,
                                asset.PurchaseDate,
                                asset.Warranty,
                                asset.Description,
                                asset.CreatedDate,
                                asset.CreatedBy,
                                asset.UpdatedDate,
                                asset.UpdatedBy,
                                asset.IsDeleted,

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
                            result.Add(asset);
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
                var query =
                    @"
                    SELECT 
                        a.*,
                        e.EmployeeName as OwnerName,
                        d.DepartmentName
                    FROM Asset a
                    LEFT JOIN Employees e ON a.OwnerId = e.EmployeeId
                    LEFT JOIN Departments d ON a.Department = d.DepartmentId
                    WHERE a.Id = @Id AND a.IsDeleted = 0";

                using (var connection = new SqlConnection(_connectionString))
                {
                    var asset = await connection.QueryFirstOrDefaultAsync<Asset>(
                        query,
                        new { Id = id }
                    );
                    if (asset == null)
                        return NotFound("ไม่พบข้อมูลทรัพย์สิน");

                    return Json(asset);
                }
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
                        SUM(CASE WHEN Status = '3' THEN 1 ELSE 0 END) as Damaged
                    FROM Assets
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
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var query =
                    @"
                    SELECT 
                        DepartmentId as Id,
                        DepartmentName as Text
                    FROM Departments
                    WHERE IsActive = 1
                    ORDER BY DepartmentName";

                using (var connection = new SqlConnection(_hrIpsConnectionString))
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
    }
}
