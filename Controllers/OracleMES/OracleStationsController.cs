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
                // ตรวจสอบ request และ CartonNo
                if (request == null)
                {
                    return Json(new { success = false, message = "ข้อมูลคำขอไม่ถูกต้อง" });
                }

                if (string.IsNullOrWhiteSpace(request.CartonNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุหมายเลขกล่อง" });
                }

                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);

                var deleteSql = "DELETE FROM g_pack_carton WHERE CARTON_NO = :cartonNo";
                var rowsAffected = await connection.ExecuteAsync(
                    deleteSql,
                    new { cartonNo = request.CartonNo }
                );

                if (rowsAffected > 0)
                    return Json(new { success = true, message = "ลบกล่องสำเร็จ" });
                else
                    return Json(new { success = false, message = "ไม่พบกล่องที่ต้องการลบ" });
            }
            catch (Exception ex)
            {
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
                var count = await connection.QueryFirstOrDefaultAsync<int>(sql, new { value });

                return Json(
                    new
                    {
                        success = true,
                        tableName = tableName,
                        columnName = columnName,
                        value = value,
                        count = count,
                        message = $"พบข้อมูล {count} รายการในตาราง {tableName} ที่ {columnName} = '{value}'",
                    }
                );
            }
            catch (Exception ex)
            {
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
                Console.WriteLine(
                    $"TestModelBinding - Request received: {JsonConvert.SerializeObject(request)}"
                );
                Console.WriteLine($"TestModelBinding - Request is null: {request == null}");
                Console.WriteLine($"TestModelBinding - CartonNo: '{request?.CartonNo}'");
                Console.WriteLine($"TestModelBinding - OpenEmpId: {request?.OpenEmpId}");
                Console.WriteLine($"TestModelBinding - Content-Type: {Request.ContentType}");
                Console.WriteLine($"TestModelBinding - Content-Length: {Request.ContentLength}");

                // ตรวจสอบ Model State
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    Console.WriteLine(
                        $"TestModelBinding - Model State Errors: {string.Join(", ", errors)}"
                    );
                }

                // อ่าน Request Body โดยตรง
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body);
                var bodyContent = await reader.ReadToEndAsync();
                Console.WriteLine($"TestModelBinding - Raw body: {bodyContent}");

                return Json(
                    new
                    {
                        success = true,
                        message = "ทดสอบ Model Binding สำเร็จ",
                        request = request,
                        modelStateValid = ModelState.IsValid,
                        modelStateErrors = ModelState
                            .Values.SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList(),
                        rawBody = bodyContent,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
