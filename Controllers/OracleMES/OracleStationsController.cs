using System.Data;
using System.Diagnostics;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.History;
using IPS_TH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;

namespace IPS_TH.Controllers
{
    public class OracleStationsController : Controller
    {
        private readonly OracleHistoryDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IOracleHistoryService _historyService;
 
        public OracleStationsController(
            OracleHistoryDbContext context,
            IConfiguration configuration,
            IOracleHistoryService historyService
        )
        {
            _context = context;
            _configuration = configuration;
            _historyService = historyService;
        }

        public IActionResult Station()
        {
            return View();
        }

        public IActionResult SetProcess()
        {
            return View();
        }

        public IActionResult PalletManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPallets(string yyyymm = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT 
                        p.PALLET_NO as PalletId,
                        p.WORK_ORDER as WorkOrder,
                        p.PART_ID as PartId,
                        p.CLOSE_FLAG as CloseFlag,
                        p.TERMINAL_ID as TerminalId,
                        p.CREATE_EMP_ID as CreateEmpId,
                        p.CREATE_TIME as CreateTime,
                        p.CLOSE_TIME as CloseTime,
                        p.CLOSE_EMP_ID as CloseEmpId,
                        p.QC_FLAG as QcFlag,
                        p.MIX_FLAG as MixFlag,
                        p.PALLET_VOLUME as PalletVolume,
                        p.FULL_FLAG as FullFlag,
                        t.TERMINAL_NAME as TerminalName,
                        COUNT(g.SERIAL_NUMBER) as SerialCount
                    FROM g_pack_pallet p
                    LEFT JOIN sys_terminal t ON p.TERMINAL_ID = t.TERMINAL_ID
                    LEFT JOIN g_sn_status g ON p.PALLET_NO = g.PALLET_NO
                    WHERE 1=1
                ";

                if (!string.IsNullOrEmpty(yyyymm))
                {
                    sql += " AND TO_CHAR(p.CREATE_TIME, 'YYYY-MM') = :yyyymm";
                }

                sql +=
                    @"
                    GROUP BY 
                        p.PALLET_NO, p.WORK_ORDER, p.PART_ID, p.CLOSE_FLAG, 
                        p.TERMINAL_ID, p.CREATE_EMP_ID, p.CREATE_TIME, p.CLOSE_TIME,
                        p.CLOSE_EMP_ID, p.QC_FLAG, p.MIX_FLAG, p.PALLET_VOLUME, 
                        p.FULL_FLAG, t.TERMINAL_NAME
                    ORDER BY p.CREATE_TIME DESC";

                var pallets = await connection.QueryAsync<PalletInfo>(sql, new { yyyymm });
                return Json(pallets);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartons(string palletNo = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT 
                        c.CARTON_NO as CartonId,
                        c.WORK_ORDER as WorkOrder,
                        c.PART_ID as PartId,
                        c.CLOSE_FLAG as CloseFlag,
                        c.TERMINAL_ID as TerminalId,
                        c.CREATE_EMP_ID as CreateEmpId,
                        c.CREATE_TIME as CreateTime,
                        c.CLOSE_TIME as CloseTime,
                        c.CLOSE_EMP_ID as CloseEmpId,
                        c.QC_FLAG as QcFlag,
                        c.MIX_FLAG as MixFlag,
                        c.FULL_FLAG as FullFlag,
                        t.TERMINAL_NAME as TerminalName,
                        COUNT(g.SERIAL_NUMBER) as SerialCount
                    FROM g_pack_carton c
                    LEFT JOIN sys_terminal t ON c.TERMINAL_ID = t.TERMINAL_ID
                    LEFT JOIN g_sn_status g ON c.CARTON_NO = g.CARTON_NO";

                if (!string.IsNullOrEmpty(palletNo))
                {
                    sql += " WHERE g.PALLET_NO = :palletNo";
                }

                sql +=
                    @" GROUP BY 
                            c.CARTON_NO, c.WORK_ORDER, c.PART_ID, c.CLOSE_FLAG, 
                            c.TERMINAL_ID, c.CREATE_EMP_ID, c.CREATE_TIME, c.CLOSE_TIME,
                            c.CLOSE_EMP_ID, c.QC_FLAG, c.MIX_FLAG, c.FULL_FLAG, t.TERMINAL_NAME
                        ORDER BY c.CREATE_TIME DESC";

                var cartons = await connection.QueryAsync<CartonInfo>(sql, new { palletNo });
                return Json(cartons);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPalletDetails(string palletNo)
        {
            try
            {
                // Debug: แสดงข้อมูลที่ได้รับ
                Console.WriteLine($"GetPalletDetails - palletNo received: '{palletNo}'");

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // ตรวจสอบว่าพาเลทมีอยู่ในฐานข้อมูลหรือไม่
                var checkPalletSql =
                    "SELECT COUNT(*) FROM g_pack_pallet WHERE PALLET_NO = :palletNo";
                var palletCount = await connection.QueryFirstOrDefaultAsync<int>(
                    checkPalletSql,
                    new { palletNo }
                );
                Console.WriteLine($"GetPalletDetails - Pallet count in database: {palletCount}");

                // ข้อมูลพาเลท
                var palletSql =
                    @"
                    SELECT 
                        p.PALLET_NO as PalletId,
                        p.WORK_ORDER as WorkOrder,
                        p.PART_ID as PartId,
                        p.CLOSE_FLAG as CloseFlag,
                        p.TERMINAL_ID as TerminalId,
                        p.CREATE_EMP_ID as CreateEmpId,
                        p.CREATE_TIME as CreateTime,
                        p.CLOSE_TIME as CloseTime,
                        p.CLOSE_EMP_ID as CloseEmpId,
                        p.QC_FLAG as QcFlag,
                        p.MIX_FLAG as MixFlag,
                        p.PALLET_VOLUME as PalletVolume,
                        p.FULL_FLAG as FullFlag,
                        t.TERMINAL_NAME as TerminalName
                    FROM g_pack_pallet p
                    LEFT JOIN sys_terminal t ON p.TERMINAL_ID = t.TERMINAL_ID
                    WHERE p.PALLET_NO = :palletNo";

                Console.WriteLine($"GetPalletDetails - SQL: {palletSql}");
                Console.WriteLine($"GetPalletDetails - Parameters: {{ palletNo: '{palletNo}' }}");

                var pallet = await connection.QueryFirstOrDefaultAsync<PalletInfo>(
                    palletSql,
                    new { palletNo }
                );

                if (pallet == null)
                {
                    Console.WriteLine(
                        $"GetPalletDetails - No pallet found for palletNo: '{palletNo}'"
                    );
                    return Json(new { success = false, message = "ไม่พบข้อมูลพาเลท" });
                }

                // ข้อมูล Serial Numbers ในพาเลท
                var serialsSql =
                    @"
                    SELECT 
                        SERIAL_NUMBER as SerialNumber,
                        WORK_ORDER as WorkOrder,
                        PART_ID as PartId,
                        CURRENT_STATUS as CurrentStatus,
                        WORK_FLAG as WorkFlag,
                        CARTON_NO as CartonNo,
                        IN_PROCESS_TIME as InProcessTime,
                        OUT_PROCESS_TIME as OutProcessTime
                    FROM g_sn_status 
                    WHERE PALLET_NO = :palletNo
                    ORDER BY IN_PROCESS_TIME DESC";

                var serials = await connection.QueryAsync<SerialInfo>(serialsSql, new { palletNo });

                // ข้อมูลกล่องในพาเลท
                var cartonsSql =
                    @"
                    SELECT DISTINCT
                        c.CARTON_NO as CartonId,
                        c.WORK_ORDER as WorkOrder,
                        c.CLOSE_FLAG as CloseFlag,
                        c.CREATE_TIME as CreateTime,
                        COUNT(g.SERIAL_NUMBER) as SerialCount
                    FROM g_pack_carton c
                    INNER JOIN g_sn_status g ON c.CARTON_NO = g.CARTON_NO
                    WHERE g.PALLET_NO = :palletNo
                    GROUP BY c.CARTON_NO, c.WORK_ORDER, c.CLOSE_FLAG, c.CREATE_TIME
                    ORDER BY c.CREATE_TIME DESC";

                Console.WriteLine($"GetPalletDetails - Cartons SQL: {cartonsSql}");
                Console.WriteLine(
                    $"GetPalletDetails - Cartons Parameters: {{ palletNo: '{palletNo}' }}"
                );

                var cartons = await connection.QueryAsync<CartonInfo>(cartonsSql, new { palletNo });

                Console.WriteLine($"GetPalletDetails - Cartons found: {cartons.Count()}");

                return Json(
                    new
                    {
                        success = true,
                        pallet = pallet,
                        serials = serials,
                        cartons = cartons,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePallet([FromBody] CreatePalletDto request)
        {
            try
            {
                // ตรวจสอบ request และข้อมูลที่จำเป็น
                if (request == null)
                {
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.PalletNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขพาเลท" });
                }

                if (string.IsNullOrWhiteSpace(request.WorkOrder))
                {
                    return Json(new { success = false, message = "กรุณาระบุ Work Order" });
                }

                if (request.PartId <= 0)
                {
                    return Json(new { success = false, message = "กรุณาระบุ Part ID ที่ถูกต้อง" });
                }

                if (request.TerminalId <= 0)
                {
                    return Json(
                        new { success = false, message = "กรุณาระบุ Terminal ID ที่ถูกต้อง" }
                    );
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // ตรวจสอบว่าพาเลทมีอยู่แล้วหรือไม่
                var checkSql = "SELECT COUNT(*) FROM g_pack_pallet WHERE PALLET_NO = :palletNo";
                var count = await connection.QueryFirstOrDefaultAsync<int>(
                    checkSql,
                    new { palletNo = request.PalletNo }
                );

                if (count > 0)
                {
                    return Json(new { success = false, message = "พาเลทนี้มีอยู่แล้วในระบบ" });
                }

                // สร้างพาเลทใหม่
                var insertSql =
                    @"
                    INSERT INTO g_pack_pallet (
                        PALLET_NO, WORK_ORDER, PART_ID, CLOSE_FLAG, TERMINAL_ID, 
                        CREATE_EMP_ID, CREATE_TIME, QC_FLAG, MIX_FLAG, FULL_FLAG
                    ) VALUES (
                        :palletNo, :workOrder, :partId, 'N', :terminalId,
                        :createEmpId, SYSDATE, :qcFlag, :mixFlag, 'N'
                    )";

                var parameters = new
                {
                    palletNo = request.PalletNo,
                    workOrder = request.WorkOrder,
                    partId = request.PartId,
                    terminalId = request.TerminalId,
                    createEmpId = request.CreateEmpId,
                    qcFlag = request.QcFlag,
                    mixFlag = request.MixFlag,
                };

                await connection.ExecuteAsync(insertSql, parameters);

                return Json(new { success = true, message = "สร้างพาเลทสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClosePallet([FromBody] ClosePalletDto request)
        {
            try
            {
                // ตรวจสอบ request และ PalletNo
                if (request == null)
                {
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.PalletNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขพาเลท" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var updateSql =
                    @"
                    UPDATE g_pack_pallet 
                    SET CLOSE_FLAG = 'Y',
                        CLOSE_TIME = SYSDATE,
                        CLOSE_EMP_ID = :closeEmpId,
                        FULL_FLAG = :fullFlag
                    WHERE PALLET_NO = :palletNo";

                var parameters = new
                {
                    palletNo = request.PalletNo,
                    closeEmpId = request.CloseEmpId,
                    fullFlag = request.FullFlag,
                };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected > 0)
                {
                    return Json(new { success = true, message = "ปิดพาเลทสำเร็จ" });
                }
                else
                {
                    return Json(new { success = false, message = "ไม่พบพาเลทที่ต้องการปิด" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFactories()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT FACTORY_ID as Id, 
                           FACTORY_NAME as Name, 
                           FACTORY_DESC as Description 
                    FROM sys_factory 
                    WHERE ENABLED = 'Y' 
                    ORDER BY FACTORY_NAME";

                var factories = await connection.QueryAsync<ProcessOption>(sql);
                return Json(factories);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStages(decimal? factoryId = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT STAGE_ID as Id, 
                           STAGE_NAME as Name, 
                           STAGE_DESC as Description,
                           FACTORY_ID
                    FROM sys_stage 
                    WHERE ENABLED = 'Y'";

                if (factoryId.HasValue)
                {
                    sql += " AND FACTORY_ID = :factoryId";
                }

                sql += " ORDER BY STAGE_NAME";

                var stages = await connection.QueryAsync<ProcessOption>(sql, new { factoryId });
                return Json(stages);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPdlines(decimal? factoryId = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT PDLINE_ID as Id, 
                           PDLINE_NAME as Name, 
                           PDLINE_DESC as Description,
                           FACTORY_ID,
                           STAGE_ID
                    FROM sys_pdline 
                    WHERE ENABLED = 'Y'";

                if (factoryId.HasValue)
                {
                    sql += " AND FACTORY_ID = :factoryId";
                }

                sql += " ORDER BY PDLINE_NAME";

                var pdlines = await connection.QueryAsync<ProcessOption>(sql, new { factoryId });
                return Json(pdlines);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProcesses(decimal? stageId = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT PROCESS_ID as Id, 
                           PROCESS_NAME as Name, 
                           PROCESS_DESC as Description,
                           STAGE_ID
                    FROM sys_process 
                    WHERE ENABLED = 'Y'";

                if (stageId.HasValue)
                {
                    sql += " AND STAGE_ID = :stageId";
                }

                sql += " ORDER BY PROCESS_NAME";

                var processes = await connection.QueryAsync<ProcessOption>(sql, new { stageId });
                return Json(processes);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTerminals(
            decimal? processId = null,
            decimal? pdlineId = null,
            decimal? stageId = null
        )
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT t.TERMINAL_ID as Id, 
                           t.TERMINAL_NAME as Name, 
                           p.PROCESS_NAME as Description,
                           t.PROCESS_ID,
                           t.PDLINE_ID,
                           t.STAGE_ID
                    FROM sys_terminal t
                    LEFT JOIN sys_process p ON t.PROCESS_ID = p.PROCESS_ID
                    WHERE t.ENABLED = 'Y'";

                var parameters = new DynamicParameters();

                if (processId.HasValue)
                {
                    sql += " AND t.PROCESS_ID = :processId";
                    parameters.Add("processId", processId);
                }

                if (pdlineId.HasValue)
                {
                    sql += " AND t.PDLINE_ID = :pdlineId";
                    parameters.Add("pdlineId", pdlineId);
                }

                if (stageId.HasValue)
                {
                    sql += " AND t.STAGE_ID = :stageId";
                    parameters.Add("stageId", stageId);
                }

                sql += " ORDER BY t.TERMINAL_NAME";

                var terminals = await connection.QueryAsync<ProcessOption>(sql, parameters);
                return Json(terminals);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoutes(decimal? factoryId = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT ROUTE_ID as Id, 
                           ROUTE_NAME as Name, 
                           ROUTE_DESC as Description,
                           FACTORY_ID
                    FROM sys_route 
                    WHERE ENABLED = 'Y'";

                if (factoryId.HasValue)
                {
                    sql += " AND FACTORY_ID = :factoryId";
                }

                sql += " ORDER BY ROUTE_NAME";

                var routes = await connection.QueryAsync<ProcessOption>(sql, new { factoryId });
                return Json(routes);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateSerialNumbers(
            [FromBody] List<string> serialNumbers
        )
        {
            try
            {
                // ตรวจสอบ serialNumbers
                if (serialNumbers == null || serialNumbers.Count == 0)
                {
                    return Json(new { success = false, message = "กรุณาระบุ Serial Numbers" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var response = new List<object>();

                foreach (var serialNumber in serialNumbers)
                {
                    try
                    {
                        var sql =
                            @"
                            SELECT 
                                g.SERIAL_NUMBER,
                                g.WORK_ORDER,
                                g.PART_ID,
                                g.CURRENT_STATUS,
                                g.WORK_FLAG,
                                g.IN_PROCESS_TIME,
                                g.OUT_PROCESS_TIME,
                                p.PROCESS_NAME as CURRENT_PROCESS_NAME,
                                t.TERMINAL_NAME as CURRENT_TERMINAL_NAME,
                                s.STAGE_NAME as CURRENT_STAGE_NAME,
                                pd.PDLINE_NAME as CURRENT_PDLINE_NAME,
                                r.ROUTE_NAME as CURRENT_ROUTE_NAME,
                                CASE 
                                    WHEN g.WORK_FLAG = '0' THEN 'ปกติ'
                                    WHEN g.WORK_FLAG = '1' THEN 'รอซ่อม'
                                    WHEN g.WORK_FLAG = '2' THEN 'ซ่อมเสร็จรอส่งออก'
                                    WHEN g.WORK_FLAG = '3' THEN 'กำลังตรวจสอบ'
                                    WHEN g.WORK_FLAG = '4' THEN 'OQC รอตรวจ'
                                    WHEN g.WORK_FLAG = '5' THEN 'เข้าคลังแล้ว'
                                    WHEN g.WORK_FLAG = '6' THEN 'WIP เบิกวัตถุดิบ'
                                    WHEN g.WORK_FLAG = '7' THEN 'ส่งออกแล้ว'
                                    ELSE g.WORK_FLAG
                                END as WORK_FLAG_DESC,
                                CASE 
                                    WHEN g.CURRENT_STATUS = '0' THEN 'ผ่าน'
                                    WHEN g.CURRENT_STATUS = '1' THEN 'รอซ่อม'
                                    WHEN g.CURRENT_STATUS = '2' THEN 'ซ่อมเสร็จรอส่งออก'
                                    WHEN g.CURRENT_STATUS = '3' THEN 'กำลังตรวจสอบ'
                                    WHEN g.CURRENT_STATUS = '4' THEN 'OQC ไม่ผ่าน'
                                    ELSE g.CURRENT_STATUS
                                END as CURRENT_STATUS_DESC
                            FROM g_sn_status g
                            LEFT JOIN sys_process p ON g.PROCESS_ID = p.PROCESS_ID
                            LEFT JOIN sys_terminal t ON g.TERMINAL_ID = t.TERMINAL_ID
                            LEFT JOIN sys_stage s ON g.STAGE_ID = s.STAGE_ID
                            LEFT JOIN sys_pdline pd ON g.PDLINE_ID = pd.PDLINE_ID
                            LEFT JOIN sys_route r ON g.ROUTE_ID = r.ROUTE_ID
                            WHERE g.SERIAL_NUMBER = :serialNumber";

                        var result = await connection.QueryFirstOrDefaultAsync(
                            sql,
                            new { serialNumber }
                        );

                        if (result != null)
                        {
                            response.Add(
                                new
                                {
                                    SerialNumber = result.SERIAL_NUMBER,
                                    WorkOrder = result.WORK_ORDER,
                                    PartId = result.PART_ID,
                                    CurrentStatus = result.CURRENT_STATUS_DESC,
                                    WorkFlag = result.WORK_FLAG_DESC,
                                    CurrentProcess = result.CURRENT_PROCESS_NAME ?? "ไม่ระบุ",
                                    CurrentTerminal = result.CURRENT_TERMINAL_NAME ?? "ไม่ระบุ",
                                    CurrentStage = result.CURRENT_STAGE_NAME ?? "ไม่ระบุ",
                                    CurrentPdline = result.CURRENT_PDLINE_NAME ?? "ไม่ระบุ",
                                    CurrentRoute = result.CURRENT_ROUTE_NAME ?? "ไม่ระบุ",
                                    InProcessTime = result.IN_PROCESS_TIME?.ToString(
                                        "dd/MM/yyyy HH:mm:ss"
                                    ) ?? "ไม่ระบุ",
                                    OutProcessTime = result.OUT_PROCESS_TIME?.ToString(
                                        "dd/MM/yyyy HH:mm:ss"
                                    ) ?? "ไม่ระบุ",
                                    Found = true,
                                }
                            );
                        }
                        else
                        {
                            response.Add(
                                new
                                {
                                    SerialNumber = serialNumber,
                                    WorkOrder = "ไม่พบ",
                                    PartId = "ไม่พบ",
                                    CurrentStatus = "ไม่พบในระบบ",
                                    WorkFlag = "ไม่พบ",
                                    CurrentProcess = "ไม่พบ",
                                    CurrentTerminal = "ไม่พบ",
                                    CurrentStage = "ไม่พบ",
                                    CurrentPdline = "ไม่พบ",
                                    CurrentRoute = "ไม่พบ",
                                    InProcessTime = "ไม่พบ",
                                    OutProcessTime = "ไม่พบ",
                                    Found = false,
                                }
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Add(
                            new
                            {
                                SerialNumber = serialNumber,
                                WorkOrder = "Error",
                                PartId = "Error",
                                CurrentStatus = $"ข้อผิดพลาด: {ex.Message}",
                                WorkFlag = "Error",
                                CurrentProcess = "Error",
                                CurrentTerminal = "Error",
                                CurrentStage = "Error",
                                CurrentPdline = "Error",
                                CurrentRoute = "Error",
                                InProcessTime = "Error",
                                OutProcessTime = "Error",
                                Found = false,
                            }
                        );
                    }
                }

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetProcess([FromBody] SetProcessDto request)
        {
            try
            {
                // ตรวจสอบ request และข้อมูลที่จำเป็น
                if (request == null)
                {
                    return Json(
                        new SetProcessResponse { Success = false, Message = "ข้อมูลคำขอไม่ถูกต้อง" }
                    );
                }

                if (request.SerialNumbers == null || request.SerialNumbers.Count == 0)
                {
                    return Json(
                        new SetProcessResponse
                        {
                            Success = false,
                            Message = "กรุณาระบุ Serial Numbers",
                        }
                    );
                }

                if (request.ProcessId <= 0)
                {
                    return Json(
                        new SetProcessResponse
                        {
                            Success = false,
                            Message = "กรุณาระบุ Process ID ที่ถูกต้อง",
                        }
                    );
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var response = new SetProcessResponse { Success = true };

                foreach (var serialNumber in request.SerialNumbers)
                {
                    try
                    {
                        // ตรวจสอบว่า Serial Number มีอยู่ใน g_sn_status หรือไม่
                        var checkSql =
                            "SELECT COUNT(*) FROM g_sn_status WHERE SERIAL_NUMBER = :serialNumber";
                        var count = await connection.QueryFirstOrDefaultAsync<int>(
                            checkSql,
                            new { serialNumber }
                        );

                        if (count == 0)
                        {
                            response.Results.Add(
                                new ProcessResult
                                {
                                    SerialNumber = serialNumber,
                                    Success = false,
                                    Message = "Serial Number ไม่พบในระบบ",
                                }
                            );
                            continue;
                        }

                        // อ่านข้อมูลปัจจุบันของ serial เพื่อตรวจสอบ current_status และ terminal
                        var currentDataSql =
                            @"
                            SELECT g.CURRENT_STATUS, g.TERMINAL_ID, t.TERMINAL_NAME
                            FROM g_sn_status g
                            LEFT JOIN sys_terminal t ON g.TERMINAL_ID = t.TERMINAL_ID
                            WHERE g.SERIAL_NUMBER = :serialNumber";

                        var currentData = await connection.QueryFirstOrDefaultAsync(
                            currentDataSql,
                            new { serialNumber }
                        );

                        // ตรวจสอบว่าสถานีที่เลือกเป็นสถานีซ่อมหรือไม่
                        var isRepairStation = false;
                        if (request.TerminalId.HasValue)
                        {
                            var terminalInfoSql =
                                "SELECT TERMINAL_NAME FROM sys_terminal WHERE TERMINAL_ID = :terminalId";
                            var terminalName = await connection.QueryFirstOrDefaultAsync<string>(
                                terminalInfoSql,
                                new { terminalId = request.TerminalId }
                            );

                            // ตรวจสอบว่าชื่อสถานีมีคำว่า "ซ่อม" หรือ "repair" หรือไม่
                            if (!string.IsNullOrEmpty(terminalName))
                            {
                                isRepairStation =
                                    terminalName.ToLower().Contains("ซ่อม")
                                    || terminalName.ToLower().Contains("repair")
                                    || terminalName.ToLower().Contains("fix")
                                    || terminalName.ToLower().Contains("maintenance");
                            }
                        }

                        // กำหนดค่า current_status ใหม่
                        string newCurrentStatus = "0"; // ค่าเริ่มต้น
                        if (currentData != null && currentData.CURRENT_STATUS == "1")
                        {
                            // ถ้าเดิมเป็นสถานะซ่อม (1) และไม่ได้ไปสถานีซ่อม ให้เปลี่ยนเป็นปกติ (0)
                            if (!isRepairStation)
                            {
                                newCurrentStatus = "0";
                            }
                            else
                            {
                                // ถ้าไปสถานีซ่อม ให้คงสถานะซ่อมไว้
                                newCurrentStatus = "1";
                            }
                        }
                        else if (currentData != null)
                        {
                            // คงสถานะเดิมไว้
                            newCurrentStatus = currentData.CURRENT_STATUS ?? "0";
                        }

                        // Update g_sn_status
                        var updateSql =
                            @"
                            UPDATE g_sn_status 
                            SET PROCESS_ID = :processId,
                                TERMINAL_ID = :terminalId,
                                ROUTE_ID = :routeId,
                                STAGE_ID = :stageId,
                                PDLINE_ID = :pdlineId,
                                NEXT_PROCESS = :processId,
                                CURRENT_STATUS = :currentStatus,
                                IN_PROCESS_TIME = SYSDATE,
                                OUT_PROCESS_TIME = NULL
                            WHERE SERIAL_NUMBER = :serialNumber";

                        var parameters = new
                        {
                            processId = request.ProcessId,
                            terminalId = request.TerminalId,
                            routeId = request.RouteId,
                            stageId = request.StageId,
                            pdlineId = request.PdlineId,
                            currentStatus = newCurrentStatus,
                            serialNumber = serialNumber,
                        };

                        var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                        if (rowsAffected > 0)
                        {
                            response.Results.Add(
                                new ProcessResult
                                {
                                    SerialNumber = serialNumber,
                                    Success = true,
                                    Message = "อัพเดทสำเร็จ",
                                }
                            );
                        }
                        else
                        {
                            response.Results.Add(
                                new ProcessResult
                                {
                                    SerialNumber = serialNumber,
                                    Success = false,
                                    Message = "ไม่สามารถอัพเดทได้",
                                }
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Results.Add(
                            new ProcessResult
                            {
                                SerialNumber = serialNumber,
                                Success = false,
                                Message = ex.Message,
                            }
                        );
                    }
                }

                var failedCount = response.Results.Count(r => !r.Success);
                var successCount = response.Results.Count(r => r.Success);

                response.Message =
                    $"อัพเดทสำเร็จ {successCount} รายการ, ล้มเหลว {failedCount} รายการ";
                response.Success = failedCount == 0;

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new SetProcessResponse { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> CloseCarton([FromBody] CloseCartonDto request)
        {
            try
            {
                // Debug: แสดงข้อมูลที่ได้รับ
                Console.WriteLine(
                    $"CloseCarton - Request received: {JsonConvert.SerializeObject(request)}"
                );

                // ตรวจสอบ request และ CartonNo
                if (request == null)
                {
                    Console.WriteLine("CloseCarton - Request is null");
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.CartonNo))
                {
                    Console.WriteLine(
                        $"CloseCarton - CartonNo is null or empty: '{request.CartonNo}'"
                    );
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // Debug: แสดง SQL และ parameters
                var updateSql =
                    @"
                    UPDATE g_pack_carton 
                    SET CLOSE_FLAG = 'Y',
                        CLOSE_TIME = SYSDATE,
                        CLOSE_EMP_ID = :closeEmpId
                    WHERE CARTON_NO = :cartonNo";

                var parameters = new
                {
                    cartonNo = request.CartonNo,
                    closeEmpId = request.CloseEmpId,
                };

                Console.WriteLine($"CloseCarton - SQL: {updateSql}");
                Console.WriteLine(
                    $"CloseCarton - Parameters: {JsonConvert.SerializeObject(parameters)}"
                );

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                Console.WriteLine($"CloseCarton - Rows affected: {rowsAffected}");

                if (rowsAffected > 0)
                    return Json(new { success = true, message = "ปิดกล่องสำเร็จ" });
                else
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการปิด" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCarton([FromBody] DeleteCartonDto request)
        {
            try
            {
                Console.WriteLine($"DeleteCarton - Request received:");
                Console.WriteLine($"  CartonNo: '{request?.CartonNo}'");

                // ตรวจสอบ request และ CartonNo
                if (request == null)
                {
                    Console.WriteLine("DeleteCarton - Request is null");
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.CartonNo))
                {
                    Console.WriteLine("DeleteCarton - CartonNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // ตรวจสอบว่ากล่องมีอยู่จริงหรือไม่
                var checkSql = "SELECT COUNT(*) FROM g_pack_carton WHERE CARTON_NO = :cartonNo";
                var cartonExists = await connection.ExecuteScalarAsync<int>(
                    checkSql,
                    new { cartonNo = request.CartonNo }
                );

                if (cartonExists == 0)
                {
                    Console.WriteLine($"DeleteCarton - Carton {request.CartonNo} not found");
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการลบ" });
                }

                // ตรวจสอบว่ากล่องมี Serial Numbers อยู่หรือไม่
                var serialCountSql = "SELECT COUNT(*) FROM g_sn_status WHERE CARTON_NO = :cartonNo";
                var serialCount = await connection.ExecuteScalarAsync<int>(
                    serialCountSql,
                    new { cartonNo = request.CartonNo }
                );

                Console.WriteLine(
                    $"DeleteCarton - Carton {request.CartonNo} has {serialCount} serial numbers"
                );

                // ถ้ามี Serial Numbers อยู่ ให้แจ้งเตือน
                if (serialCount > 0)
                {
                    Console.WriteLine(
                        $"DeleteCarton - Warning: Carton {request.CartonNo} still has {serialCount} serial numbers"
                    );
                    return Json(
                        new
                        {
                            success = false,
                            message = $"ไม่สามารถลบกล่องได้ เนื่องจากยังมี Serial Numbers อยู่ {serialCount} รายการ",
                        }
                    );
                }

                // ลบกล่อง
                var deleteSql = "DELETE FROM g_pack_carton WHERE CARTON_NO = :cartonNo";
                var rowsAffected = await connection.ExecuteAsync(
                    deleteSql,
                    new { cartonNo = request.CartonNo }
                );

                Console.WriteLine(
                    $"DeleteCarton - Deleted {rowsAffected} rows for carton {request.CartonNo}"
                );

                if (rowsAffected > 0)
                {
                    return Json(new { success = true, message = "ลบกล่องสำเร็จ" });
                }
                else
                {
                    return Json(new { success = false, message = "ไม่สามารถลบกล่องได้" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteCarton - Error: {ex.Message}");
                Console.WriteLine($"DeleteCarton - Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePallet([FromBody] DeletePalletDto request)
        {
            try
            {
                // ตรวจสอบ request และ PalletNo
                if (request == null)
                {
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.PalletNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขพาเลท" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var deleteSql = "DELETE FROM g_pack_pallet WHERE PALLET_NO = :palletNo";
                var rowsAffected = await connection.ExecuteAsync(
                    deleteSql,
                    new { palletNo = request.PalletNo }
                );

                if (rowsAffected > 0)
                    return Json(new { success = true, message = "ลบพาเลทสำเร็จ" });
                else
                    return Json(new { success = false, message = "ไม่พบพาเลทที่ต้องการลบ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OpenCarton(string cartonNo, string openEmpId)
        {
            try
            {
                // Debug: แสดงข้อมูลที่ได้รับ
                Console.WriteLine($"OpenCarton - cartonNo received: '{cartonNo}'");
                Console.WriteLine($"OpenCarton - openEmpId received: '{openEmpId}'");
                Console.WriteLine($"OpenCarton - Content-Type: {Request.ContentType}");

                // ตรวจสอบข้อมูล
                if (string.IsNullOrWhiteSpace(cartonNo))
                {
                    Console.WriteLine("OpenCarton - cartonNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                var request = new OpenCartonDto { CartonNo = cartonNo };
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // Debug: แสดง SQL และ parameters
                var updateSql =
                    @"
                    UPDATE g_pack_carton 
                    SET CLOSE_FLAG = 'N',
                        CLOSE_TIME = NULL,
                        CLOSE_EMP_ID = NULL
                    WHERE CARTON_NO = :cartonNo";

                var parameters = new { cartonNo = request.CartonNo };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected > 0)
                    return Json(new { success = true, message = "เปิดกล่องสำเร็จ" });
                else
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการเปิด" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OpenPallet(string palletNo, string openEmpId)
        {
            try
            {
                // Debug: แสดงข้อมูลที่ได้รับ
                Console.WriteLine($"OpenPallet - palletNo received: '{palletNo}'");
                Console.WriteLine($"OpenPallet - openEmpId received: '{openEmpId}'");
                Console.WriteLine($"OpenPallet - Content-Type: {Request.ContentType}");

                // ตรวจสอบข้อมูล
                if (string.IsNullOrWhiteSpace(palletNo))
                {
                    Console.WriteLine("OpenPallet - palletNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขพาเลท" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var updateSql =
                    @"
                    UPDATE g_pack_pallet 
                    SET CLOSE_FLAG = 'N',
                        CLOSE_TIME = NULL,
                        CLOSE_EMP_ID = NULL, 
                        FULL_FLAG = 'N'
                    WHERE PALLET_NO = :palletNo";

                var parameters = new { palletNo = palletNo };

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);

                if (rowsAffected > 0)
                    return Json(new { success = true, message = "เปิดพาเลทสำเร็จ" });
                else
                    return Json(new { success = false, message = "ไม่พบพาเลทที่ต้องการเปิด" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับตรวจสอบข้อมูลในฐานข้อมูล
        [HttpGet]
        public async Task<IActionResult> CheckData(
            string tableName,
            string columnName,
            string value
        )
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql = $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = :value";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { value });

                return Json(
                    new
                    {
                        success = true,
                        message = $"พบข้อมูล {count} รายการในตาราง {tableName} ที่ {columnName} = '{value}'",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับค้นหาพาเลท
        [HttpGet]
        public async Task<IActionResult> SearchPallet(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return Json(new { success = false, message = "กรุณาระบุคำค้นหา" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT 
                        p.PALLET_NO as PalletId,
                        p.WORK_ORDER as WorkOrder,
                        p.PART_ID as PartId,
                        p.CLOSE_FLAG as CloseFlag,
                        p.CREATE_TIME as CreateTime,
                        p.CLOSE_TIME as CloseTime,
                        t.TERMINAL_NAME as TerminalName,
                        (SELECT COUNT(*) FROM g_sn_status s WHERE s.PALLET_NO = p.PALLET_NO) as SerialCount
                    FROM g_pack_pallet p
                    LEFT JOIN sys_terminal t ON p.TERMINAL_ID = t.TERMINAL_ID
                    WHERE p.PALLET_NO = :searchTerm 
                       OR p.WORK_ORDER = :searchTerm
                    ORDER BY p.CREATE_TIME DESC";

                var pallet = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    sql,
                    new { searchTerm }
                );

                if (pallet != null)
                {
                    return Json(new { success = true, pallet });
                }
                else
                {
                    return Json(new { success = false, message = "ไม่พบพาเลทที่ต้องการ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับค้นหากล่อง
        [HttpGet]
        public async Task<IActionResult> SearchCarton(string searchTerm)
        {
            try
            {
                Console.WriteLine($"SearchCarton - Request received: '{searchTerm}'");

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    Console.WriteLine("SearchCarton - searchTerm is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุคำค้นหา" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var sql =
                    @"
                    SELECT DISTINCT
                        c.CARTON_NO as CartonId,
                        c.WORK_ORDER as WorkOrder,
                        c.PART_ID as PartId,
                        c.CLOSE_FLAG as CloseFlag,
                        c.CREATE_TIME as CreateTime,
                        c.CLOSE_TIME as CloseTime,
                        COALESCE(s.PALLET_NO, 'ไม่ระบุ') as PalletId,
                        (SELECT COUNT(*) FROM g_sn_status s2 WHERE s2.CARTON_NO = c.CARTON_NO) as SerialCount
                    FROM g_pack_carton c
                    LEFT JOIN g_sn_status s ON c.CARTON_NO = s.CARTON_NO
                    WHERE c.CARTON_NO = :searchTerm 
                       OR c.WORK_ORDER = :searchTerm
                    ORDER BY c.CREATE_TIME DESC";

                Console.WriteLine($"SearchCarton - SQL: {sql}");
                Console.WriteLine($"SearchCarton - Parameters: searchTerm = '{searchTerm}'");

                var carton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    sql,
                    new { searchTerm }
                );

                Console.WriteLine(
                    $"SearchCarton - Query result: {(carton != null ? "Found" : "Not found")}"
                );
                if (carton != null)
                {
                    Console.WriteLine($"SearchCarton - CartonId: {carton.CartonId}");
                    Console.WriteLine($"SearchCarton - WorkOrder: {carton.WorkOrder}");
                    Console.WriteLine($"SearchCarton - PartId: {carton.PartId}");
                    Console.WriteLine($"SearchCarton - CloseFlag: {carton.CloseFlag}");
                    Console.WriteLine($"SearchCarton - PalletId: {carton.PalletId}");
                    Console.WriteLine($"SearchCarton - SerialCount: {carton.SerialCount}");
                }

                if (carton != null)
                {
                    // ส่งข้อมูลกลับในรูปแบบที่ถูกต้อง
                    var result = new
                    {
                        success = true,
                        carton = new
                        {
                            CartonId = carton.CARTONID,
                            WorkOrder = carton.WORKORDER,
                            PartId = carton.PARTID,
                            CloseFlag = carton.CLOSEFLAG,
                            CreateTime = carton.CREATETIME,
                            CloseTime = carton.CLOSETIME,
                            PalletId = carton.PALLETID,
                            SerialCount = carton.SERIALCOUNT,
                        },
                    };

                    Console.WriteLine(
                        $"SearchCarton - Returning result: {JsonConvert.SerializeObject(result)}"
                    );
                    return Json(result);
                }
                else
                {
                    Console.WriteLine("SearchCarton - No carton found");
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการ" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchCarton - Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับทดสอบ API โดยตรง
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> TestOpenCarton([FromBody] object requestData)
        {
            try
            {
                Console.WriteLine(
                    $"TestOpenCarton - Raw request data: {JsonConvert.SerializeObject(requestData)}"
                );
                Console.WriteLine($"TestOpenCarton - Request type: {requestData?.GetType().Name}");
                Console.WriteLine($"TestOpenCarton - Content-Type: {Request.ContentType}");

                // ลองแปลงเป็น OpenCartonDto
                var jsonString = JsonConvert.SerializeObject(requestData);
                var openCartonDto = JsonConvert.DeserializeObject<OpenCartonDto>(jsonString);

                Console.WriteLine(
                    $"TestOpenCarton - Deserialized DTO: {JsonConvert.SerializeObject(openCartonDto)}"
                );

                return Json(
                    new
                    {
                        success = true,
                        message = "ทดสอบสำเร็จ",
                        originalData = requestData,
                        deserializedDto = openCartonDto,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับทดสอบ Model Binding
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> TestModelBinding([FromBody] OpenCartonDto request)
        {
            try
            {
                Console.WriteLine($"TestModelBinding - Request received:");
                Console.WriteLine($"  CartonNo: '{request?.CartonNo}'");
                Console.WriteLine($"  OpenEmpId: '{request?.OpenEmpId}'");
                Console.WriteLine($"  Content-Type: {Request.ContentType}");
                Console.WriteLine($"  Content-Length: {Request.ContentLength}");

                if (Request.Body.CanSeek)
                {
                    Request.Body.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(Request.Body);
                    var bodyContent = await reader.ReadToEndAsync();
                    Console.WriteLine($"  Raw Body Content: '{bodyContent}'");
                }

                return Json(
                    new
                    {
                        success = true,
                        message = "Model Binding Test Successful",
                        receivedData = request,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับย้ายกล่อง
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> MoveCarton([FromBody] MoveCartonDto request)
        {
            try
            {
                Console.WriteLine($"MoveCarton - Request received:");
                Console.WriteLine($"  CartonNo: '{request?.CartonNo}'");
                Console.WriteLine($"  TargetPalletNo: '{request?.TargetPalletNo}'");
                Console.WriteLine($"  MoveEmpId: '{request?.MoveEmpId}'");

                // ตรวจสอบข้อมูล
                if (string.IsNullOrWhiteSpace(request?.CartonNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                if (string.IsNullOrWhiteSpace(request?.TargetPalletNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุพาเลทปลายทาง" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // เริ่ม transaction
                using var transaction = connection.BeginTransaction();

                try
                {
                    // ตรวจสอบว่ากล่องมีอยู่จริง
                    var cartonSql =
                        @"
                        SELECT 
                            c.CARTON_NO,
                            c.PALLET_NO as SourcePalletNo,
                            c.WORK_ORDER,
                            c.CLOSE_FLAG,
                            (SELECT COUNT(*) FROM g_sn_status s WHERE s.CARTON_NO = c.CARTON_NO) as SerialCount
                        FROM g_pack_carton c
                        WHERE c.CARTON_NO = :cartonNo";

                    var carton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        cartonSql,
                        new { cartonNo = request.CartonNo }
                    );

                    if (carton == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการย้าย" });
                    }

                    // ตรวจสอบว่าพาเลทปลายทางมีอยู่จริง
                    var targetPalletSql =
                        @"
                        SELECT PALLET_NO, WORK_ORDER, CLOSE_FLAG
                        FROM g_pack_pallet
                        WHERE PALLET_NO = :palletNo";

                    var targetPallet = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        targetPalletSql,
                        new { palletNo = request.TargetPalletNo }
                    );

                    if (targetPallet == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "ไม่พบพาเลทปลายทาง" });
                    }

                    // ตรวจสอบว่าไม่ย้ายไปยังพาเลทเดียวกัน
                    if (carton.SourcePalletNo == request.TargetPalletNo)
                    {
                        transaction.Rollback();
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่สามารถย้ายกล่องไปยังพาเลทเดียวกันได้",
                            }
                        );
                    }

                    // ตรวจสอบว่าพาเลทปลายทางยังไม่ปิด
                    if (targetPallet.CloseFlag == "Y")
                    {
                        transaction.Rollback();
                        return Json(
                            new
                            {
                                success = false,
                                message = "ไม่สามารถย้ายกล่องไปยังพาเลทที่ปิดแล้วได้",
                            }
                        );
                    }

                    // อัปเดตกล่องให้ย้ายไปยังพาเลทปลายทาง
                    var updateCartonSql =
                        @"
                        UPDATE g_pack_carton 
                        SET PALLET_NO = :targetPalletNo,
                            UPDATE_TIME = SYSDATE,
                            UPDATE_EMP_ID = :moveEmpId
                        WHERE CARTON_NO = :cartonNo";

                    var cartonUpdateParams = new
                    {
                        targetPalletNo = request.TargetPalletNo,
                        moveEmpId = request.MoveEmpId,
                        cartonNo = request.CartonNo,
                    };

                    var cartonRowsAffected = await connection.ExecuteAsync(
                        updateCartonSql,
                        cartonUpdateParams,
                        transaction
                    );

                    if (cartonRowsAffected == 0)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "ไม่สามารถอัปเดตกล่องได้" });
                    }

                    // อัปเดต Serial Numbers ทั้งหมดในกล่องให้ย้ายไปยังพาเลทปลายทาง
                    var updateSerialSql =
                        @"
                        UPDATE g_sn_status 
                        SET PALLET_NO = :targetPalletNo,
                            UPDATE_TIME = SYSDATE,
                            UPDATE_EMP_ID = :moveEmpId
                        WHERE CARTON_NO = :cartonNo";

                    var serialUpdateParams = new
                    {
                        targetPalletNo = request.TargetPalletNo,
                        moveEmpId = request.MoveEmpId,
                        cartonNo = request.CartonNo,
                    };

                    var serialRowsAffected = await connection.ExecuteAsync(
                        updateSerialSql,
                        serialUpdateParams,
                        transaction
                    );

                    // Commit transaction
                    transaction.Commit();

                    return Json(
                        new
                        {
                            success = true,
                            message = $"ย้ายกล่อง {request.CartonNo} ไปยังพาเลท {request.TargetPalletNo} สำเร็จ (Serial Numbers: {serialRowsAffected} รายการ)",
                        }
                    );
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับย้าย Serial Numbers
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> MoveSerialNumbers([FromBody] MoveSerialNumbersDto request)
        {
            try
            {
                Console.WriteLine($"MoveSerialNumbers - Request received:");
                Console.WriteLine($"  Request object: {request}");
                Console.WriteLine($"  SourceCartonNo: '{request?.SourceCartonNo}'");
                Console.WriteLine($"  TargetCartonNo: '{request?.TargetCartonNo}'");
                Console.WriteLine($"  MoveEmpId: '{request?.MoveEmpId}'");
                Console.WriteLine($"  Request type: {request?.GetType()}");

                // ตรวจสอบว่า request เป็น null หรือไม่
                if (request == null)
                {
                    Console.WriteLine("MoveSerialNumbers - Request is null");
                    return Json(
                        new
                        {
                            success = false,
                            message = "ไม่ได้รับข้อมูลที่ส่งมา (Request is null)",
                        }
                    );
                }

                // ตรวจสอบข้อมูล
                if (string.IsNullOrWhiteSpace(request.SourceCartonNo))
                {
                    Console.WriteLine("MoveSerialNumbers - SourceCartonNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุกล่องต้นทาง" });
                }

                if (string.IsNullOrWhiteSpace(request.TargetCartonNo))
                {
                    Console.WriteLine("MoveSerialNumbers - TargetCartonNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุกล่องปลายทาง" });
                }

                // ตรวจสอบ MoveEmpId
                if (request.MoveEmpId <= 0)
                {
                    Console.WriteLine("MoveSerialNumbers - MoveEmpId is invalid");
                    return Json(
                        new { success = false, message = "กรุณาระบุรหัสพนักงานที่ถูกต้อง" }
                    );
                }

                Console.WriteLine($"MoveSerialNumbers - All validations passed:");
                Console.WriteLine($"  SourceCartonNo: '{request.SourceCartonNo}'");
                Console.WriteLine($"  TargetCartonNo: '{request.TargetCartonNo}'");
                Console.WriteLine($"  MoveEmpId: {request.MoveEmpId}");

                var connectionString = _configuration.GetConnectionString("OracleConnection");

                using (var connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("MoveSerialNumbers - Connection opened successfully");
                    Console.WriteLine($"MoveSerialNumbers - Connection state: {connection.State}");

                    // ตรวจสอบ connection state ก่อนเริ่ม transaction
                    if (connection.State != System.Data.ConnectionState.Open)
                    { 
                        Console.WriteLine(
                            "MoveSerialNumbers - Connection is not open, attempting to open again"
                        );
                        await connection.OpenAsync();
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        Console.WriteLine("MoveSerialNumbers - Transaction started successfully");

                        try
                        {
                            // ตรวจสอบว่ากล่องต้นทางมีอยู่จริง
                            var sourceCartonSql =
                                @"
                        SELECT 
                            c.CARTON_NO,
                            c.WORK_ORDER,
                            c.CLOSE_FLAG,
                            (SELECT COUNT(*) FROM g_sn_status s WHERE s.CARTON_NO = c.CARTON_NO) as SerialCount
                        FROM g_pack_carton c
                        WHERE c.CARTON_NO = :cartonNo";

                            var sourceCarton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                sourceCartonSql,
                                new { cartonNo = request.SourceCartonNo }
                            );

                            if (sourceCarton == null)
                            {
                                transaction.Rollback();
                                return Json(new { success = false, message = "ไม่พบกล่องต้นทาง" });
                            }

                            // ตรวจสอบว่ากล่องปลายทางมีอยู่จริง
                            var targetCartonSql =
                                @"
                        SELECT 
                            c.CARTON_NO,
                            c.WORK_ORDER,
                            c.CLOSE_FLAG
                        FROM g_pack_carton c
                        WHERE c.CARTON_NO = :cartonNo";

                            var targetCarton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                targetCartonSql,
                                new { cartonNo = request.TargetCartonNo }
                            );

                            if (targetCarton == null)
                            {
                                transaction.Rollback();
                                return Json(new { success = false, message = "ไม่พบกล่องปลายทาง" });
                            }

                            // ตรวจสอบว่าไม่ย้ายไปยังกล่องเดียวกัน
                            if (request.SourceCartonNo == request.TargetCartonNo)
                            {
                                transaction.Rollback();
                                return Json(
                                    new
                                    {
                                        success = false,
                                        message = "ไม่สามารถย้าย Serial Numbers ไปยังกล่องเดียวกันได้",
                                    }
                                );
                            }

                            // ตรวจสอบว่ากล่องปลายทางยังไม่ปิด
                            if (targetCarton.CloseFlag == "Y")
                            {
                                transaction.Rollback();
                                return Json(
                                    new
                                    {
                                        success = false,
                                        message = "ไม่สามารถย้าย Serial Numbers ไปยังกล่องที่ปิดแล้วได้",
                                    }
                                );
                            }

                            // ตรวจสอบว่ากล่องต้นทางมี Serial Numbers หรือไม่
                            Console.WriteLine(
                                $"MoveSerialNumbers - Source carton serial count: {sourceCarton.SerialCount}"
                            );
                            if (sourceCarton.SerialCount == 0)
                            {
                                transaction.Rollback();
                                Console.WriteLine(
                                    "MoveSerialNumbers - Source carton has no serial numbers"
                                );
                                return Json(
                                    new
                                    {
                                        success = false,
                                        message = "กล่องต้นทางไม่มี Serial Numbers ที่จะย้าย",
                                    }
                                );
                            }

                            // หา PALLET_NO ของกล่องปลายทางจาก g_sn_status
                            var targetPalletSql =
                                @"
                        SELECT DISTINCT PALLET_NO 
                        FROM g_sn_status 
                        WHERE CARTON_NO = :cartonNo 
                        AND ROWNUM = 1";

                            var targetPallet = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                targetPalletSql,
                                new { cartonNo = request.TargetCartonNo }
                            );

                            string targetPalletNo = null;
                            if (targetPallet != null)
                            {
                                targetPalletNo = targetPallet.PALLET_NO;
                            }
                            else
                            {
                                // ถ้าไม่มี Serial Numbers ในกล่องปลายทาง ให้ใช้ PALLET_NO จากกล่องต้นทาง
                                var sourcePalletSql =
                                    @"
                            SELECT DISTINCT PALLET_NO 
                            FROM g_sn_status 
                            WHERE CARTON_NO = :cartonNo 
                            AND ROWNUM = 1";

                                var sourcePallet =
                                    await connection.QueryFirstOrDefaultAsync<dynamic>(
                                        sourcePalletSql,
                                        new { cartonNo = request.SourceCartonNo }
                                    );
                                targetPalletNo = sourcePallet?.PalletNo;
                            }

                            if (string.IsNullOrEmpty(targetPalletNo))
                            {
                                transaction.Rollback();
                                return Json(
                                    new
                                    {
                                        success = false,
                                        message = "ไม่สามารถหา PALLET_NO สำหรับกล่องปลายทางได้",
                                    }
                                );
                            }

                            // อัปเดต Serial Numbers ทั้งหมดจากกล่องต้นทางไปยังกล่องปลายทาง
                            var updateSerialSql =
                                @"
                        UPDATE g_sn_status 
                        SET CARTON_NO = :targetCartonNo,
                            PALLET_NO = :targetPalletNo
                        WHERE CARTON_NO = :sourceCartonNo";

                            var serialUpdateParams = new
                            {
                                targetCartonNo = request.TargetCartonNo,
                                targetPalletNo = targetPalletNo,
                                sourceCartonNo = request.SourceCartonNo,
                            };

                            var serialRowsAffected = await connection.ExecuteAsync(
                                updateSerialSql,
                                serialUpdateParams,
                                transaction
                            );

                            if (serialRowsAffected == 0)
                            {
                                transaction.Rollback();
                                return Json(
                                    new
                                    {
                                        success = false,
                                        message = "ไม่พบ Serial Numbers ที่จะย้าย",
                                    }
                                );
                            }

                            // Commit transaction
                            transaction.Commit();

                            Console.WriteLine(
                                $"MoveSerialNumbers - Successfully moved {serialRowsAffected} serial numbers"
                            );
                            Console.WriteLine(
                                $"MoveSerialNumbers - From carton: {request.SourceCartonNo} to carton: {request.TargetCartonNo}"
                            );

                            return Json(
                                new
                                {
                                    success = true,
                                    message = $"ย้าย Serial Numbers จากกล่อง {request.SourceCartonNo} ไปยังกล่อง {request.TargetCartonNo} สำเร็จ (Serial Numbers: {serialRowsAffected} รายการ)",
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                transaction.Rollback();
                                Console.WriteLine("MoveSerialNumbers - Transaction rolled back");
                            }
                            catch (Exception rollbackEx)
                            {
                                Console.WriteLine(
                                    $"MoveSerialNumbers - Error during rollback: {rollbackEx.Message}"
                                );
                            }

                            Console.WriteLine(
                                $"MoveSerialNumbers - Transaction error: {ex.Message}"
                            );
                            Console.WriteLine($"MoveSerialNumbers - Stack trace: {ex.StackTrace}");

                            return Json(
                                new
                                {
                                    success = false,
                                    message = "เกิดข้อผิดพลาดในการประมวลผล: " + ex.Message,
                                }
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MoveSerialNumbers - General error: {ex.Message}");
                Console.WriteLine($"MoveSerialNumbers - Stack trace: {ex.StackTrace}");

                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับย้าย Serial Numbers ไปยัง Pallet โดยตรง
        [HttpPost]
        public async Task<IActionResult> MoveSerialNumbersToPallet([FromBody] MoveSerialNumbersToPalletDto request)
        {
            try
            {
                Console.WriteLine($"MoveSerialNumbersToPallet - Request received:");
                Console.WriteLine($"SourceCartonNo: {request?.SourceCartonNo}");
                Console.WriteLine($"TargetPalletNo: {request?.TargetPalletNo}");
                Console.WriteLine($"MoveEmpId: {request?.MoveEmpId}");

                if (request == null)
                {
                    Console.WriteLine("MoveSerialNumbersToPallet - Request is null");
                    return Json(new { success = false, message = "ข้อมูลที่ส่งมาไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.SourceCartonNo))
                {
                    Console.WriteLine("MoveSerialNumbersToPallet - SourceCartonNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่องต้นทาง" });
                }

                if (string.IsNullOrWhiteSpace(request.TargetPalletNo))
                {
                    Console.WriteLine("MoveSerialNumbersToPallet - TargetPalletNo is null or empty");
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขพาเลทปลายทาง" });
                }

                // ตรวจสอบว่าไม่ย้ายไปยังพาเลทเดียวกัน
                if (request.SourceCartonNo == request.TargetPalletNo)
                {
                    Console.WriteLine("MoveSerialNumbersToPallet - Source carton and target pallet are the same");
                    return Json(new { success = false, message = "ไม่สามารถเปลี่ยนไปยังพาเลทเดียวกันได้" });
                }

                if (request.MoveEmpId <= 0)
                {
                    Console.WriteLine("MoveSerialNumbersToPallet - MoveEmpId is invalid");
                    return Json(new { success = false, message = "กรุณาระบุรหัสพนักงานที่ย้าย" });
                }

                Console.WriteLine($"MoveSerialNumbersToPallet - All validations passed:");
                Console.WriteLine($"SourceCartonNo: {request.SourceCartonNo}");
                Console.WriteLine($"TargetPalletNo: {request.TargetPalletNo}");
                Console.WriteLine($"MoveEmpId: {request.MoveEmpId}");
                Console.WriteLine($"MoveSerialNumbersToPallet - This will change PALLET_NO only, CARTON_NO remains the same");

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                await connection.OpenAsync();
                Console.WriteLine("MoveSerialNumbersToPallet - Connection opened successfully");
                Console.WriteLine($"MoveSerialNumbersToPallet - Connection state: {connection.State}");

                if (connection.State != ConnectionState.Open)
                {
                    Console.WriteLine(
                        "MoveSerialNumbersToPallet - Connection is not open, attempting to open again"
                    );
                    await connection.OpenAsync();
                }

                using var transaction = connection.BeginTransaction();
                Console.WriteLine("MoveSerialNumbersToPallet - Transaction started successfully");

                try
                {
                    // ตรวจสอบว่ากล่องต้นทางมีอยู่จริง
                    var sourceCartonSql = @"
                        SELECT 
                            c.CARTON_NO as CartonNo,
                            c.WORK_ORDER as WorkOrder,
                            c.PART_ID as PartId,
                            c.CLOSE_FLAG as CloseFlag,
                            c.CREATE_TIME as CreateTime,
                            (SELECT COUNT(*) FROM g_sn_status s WHERE s.CARTON_NO = c.CARTON_NO) as SerialCount
                        FROM g_pack_carton c
                        WHERE c.CARTON_NO = :cartonNo";

                    var sourceCarton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        sourceCartonSql,
                        new { cartonNo = request.SourceCartonNo },
                        transaction
                    );

                    if (sourceCarton == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "ไม่พบกล่องต้นทางที่ต้องการ" });
                    }

                    Console.WriteLine(
                        $"MoveSerialNumbersToPallet - Source carton serial count: {sourceCarton.SerialCount}"
                    );

                                    if (sourceCarton.SerialCount == 0)
                {
                    transaction.Rollback();
                    Console.WriteLine("MoveSerialNumbersToPallet - Source carton has no serial numbers");
                    return Json(new { success = false, message = "กล่องต้นทางไม่มี Serial Numbers ที่จะเปลี่ยน Pallet" });
                }

                    // ตรวจสอบว่าพาเลทปลายทางมีอยู่จริง
                    var targetPalletSql = @"
                        SELECT 
                            p.PALLET_NO as PalletNo,
                            p.WORK_ORDER as WorkOrder,
                            p.PART_ID as PartId,
                            p.CLOSE_FLAG as CloseFlag
                        FROM g_pack_pallet p
                        WHERE p.PALLET_NO = :palletNo";

                    var targetPallet = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        targetPalletSql,
                        new { palletNo = request.TargetPalletNo },
                        transaction
                    );

                    if (targetPallet == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "ไม่พบพาเลทปลายทางที่ต้องการ" });
                    }

                    // ย้าย Serial Numbers จากกล่องไปยังพาเลท
                    var moveSerialSql = @"
                        UPDATE g_sn_status 
                        SET 
                            PALLET_NO = :targetPalletNo 
                        WHERE CARTON_NO = :sourceCartonNo";

                    var serialRowsAffected = await connection.ExecuteAsync(
                        moveSerialSql,
                        new
                        {
                            targetPalletNo = request.TargetPalletNo,
                            sourceCartonNo = request.SourceCartonNo
                        },
                        transaction
                    );

                    Console.WriteLine(
                        $"MoveSerialNumbersToPallet - Successfully changed PALLET_NO for {serialRowsAffected} serial numbers"
                    );
                    Console.WriteLine(
                        $"MoveSerialNumbersToPallet - Serial numbers in carton {request.SourceCartonNo} now belong to pallet {request.TargetPalletNo}"
                    );

                    // บันทึกการเปลี่ยนแปลง
                    transaction.Commit();
                    Console.WriteLine("MoveSerialNumbersToPallet - Transaction committed successfully");

                    return Json(new
                    {
                        success = true,
                        message = $"เปลี่ยน Pallet ของ Serial Numbers สำเร็จ {serialRowsAffected} รายการ จากกล่อง {request.SourceCartonNo} ไปยังพาเลท {request.TargetPalletNo}",
                        serialCount = serialRowsAffected
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MoveSerialNumbersToPallet - Transaction error: {ex.Message}");
                    Console.WriteLine($"MoveSerialNumbersToPallet - Stack trace: {ex.StackTrace}");

                    try
                    {
                        transaction.Rollback();
                        Console.WriteLine("MoveSerialNumbersToPallet - Transaction rolled back");
                    }
                    catch (Exception rollbackEx)
                    {
                        Console.WriteLine($"MoveSerialNumbersToPallet - Error during rollback: {rollbackEx.Message}");
                    }

                    return Json(new { success = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MoveSerialNumbersToPallet - General error: {ex.Message}");
                Console.WriteLine($"MoveSerialNumbersToPallet - Stack trace: {ex.StackTrace}");

                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับดูประวัติการย้ายกล่อง
        [HttpGet]
        public async Task<IActionResult> GetMoveHistory(string cartonNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cartonNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // ตรวจสอบว่ากล่องมีอยู่จริง
                var cartonExistsSql =
                    "SELECT COUNT(*) FROM g_pack_carton WHERE CARTON_NO = :cartonNo";
                var cartonExists = await connection.ExecuteScalarAsync<int>(
                    cartonExistsSql,
                    new { cartonNo }
                );

                if (cartonExists == 0)
                {
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการ" });
                }

                // เนื่องจากไม่มีตารางประวัติ ให้ส่งข้อมูลว่างกลับไป
                return Json(
                    new
                    {
                        success = true,
                        history = new List<object>(),
                        message = "ไม่พบประวัติการย้ายสำหรับกล่องนี้",
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เมธอดสำหรับทดสอบ SearchCarton
        [HttpGet]
        public async Task<IActionResult> TestSearchCarton(string searchTerm = "C0307834271037")
        {
            try
            {
                Console.WriteLine($"TestSearchCarton - Testing with searchTerm: '{searchTerm}'");

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                // ทดสอบ query ง่ายๆ ก่อน
                var testSql =
                    "SELECT COUNT(*) as Count FROM g_pack_carton WHERE CARTON_NO = :searchTerm";
                var count = await connection.ExecuteScalarAsync<int>(testSql, new { searchTerm });
                Console.WriteLine(
                    $"TestSearchCarton - Found {count} cartons with CARTON_NO = '{searchTerm}'"
                );

                // ทดสอบ query หลัก
                var sql =
                    @"
                    SELECT DISTINCT
                        c.CARTON_NO as CartonId,
                        c.WORK_ORDER as WorkOrder,
                        c.PART_ID as PartId,
                        c.CLOSE_FLAG as CloseFlag,
                        c.CREATE_TIME as CreateTime,
                        c.CLOSE_TIME as CloseTime,
                        COALESCE(s.PALLET_NO, 'ไม่ระบุ') as PalletId,
                        (SELECT COUNT(*) FROM g_sn_status s2 WHERE s2.CARTON_NO = c.CARTON_NO) as SerialCount
                    FROM g_pack_carton c
                    LEFT JOIN g_sn_status s ON c.CARTON_NO = s.CARTON_NO
                    WHERE c.CARTON_NO = :searchTerm 
                       OR c.WORK_ORDER = :searchTerm
                    ORDER BY c.CREATE_TIME DESC";

                var carton = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    sql,
                    new { searchTerm }
                );

                if (carton != null)
                {
                    var result = new
                    {
                        success = true,
                        carton = new
                        {
                            CartonId = carton.CartonId,
                            WorkOrder = carton.WorkOrder,
                            PartId = carton.PartId,
                            CloseFlag = carton.CloseFlag,
                            CreateTime = carton.CreateTime,
                            CloseTime = carton.CloseTime,
                            PalletId = carton.PalletId,
                            SerialCount = carton.SerialCount,
                        },
                    };

                    Console.WriteLine(
                        $"TestSearchCarton - Success: {JsonConvert.SerializeObject(result)}"
                    );
                    return Json(result);
                }
                else
                {
                    Console.WriteLine("TestSearchCarton - No carton found");
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการ" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestSearchCarton - Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
