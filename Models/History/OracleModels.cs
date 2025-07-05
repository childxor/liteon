using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPS_TH.Models.History
{
    [Table("g_sn_status")]
    public class GSnStatus
    {
        [Key]
        [Column("SERIAL_NUMBER")]
        public string SerialNumber { get; set; } = string.Empty;

        [Column("WORK_ORDER")]
        public string? WorkOrder { get; set; }

        [Column("PART_ID")]
        public decimal? PartId { get; set; }

        [Column("VERSION")]
        public string? Version { get; set; }

        [Column("ROUTE_ID")]
        public decimal? RouteId { get; set; }

        [Column("PDLINE_ID")]
        public decimal? PdlineId { get; set; }

        [Column("STAGE_ID")]
        public decimal? StageId { get; set; }

        [Column("PROCESS_ID")]
        public decimal? ProcessId { get; set; }

        [Column("TERMINAL_ID")]
        public decimal? TerminalId { get; set; }

        [Column("NEXT_PROCESS")]
        public decimal? NextProcess { get; set; }

        [Column("CURRENT_STATUS")]
        public string? CurrentStatus { get; set; }

        [Column("WORK_FLAG")]
        public string? WorkFlag { get; set; }

        [Column("IN_PROCESS_TIME")]
        public DateTime? InProcessTime { get; set; }

        [Column("OUT_PROCESS_TIME")]
        public DateTime? OutProcessTime { get; set; }

        [Column("EMP_ID")]
        public decimal? EmpId { get; set; }
    }

    [Table("sys_terminal")]
    public class SysTerminal
    {
        [Key]
        [Column("TERMINAL_ID")]
        public decimal TerminalId { get; set; }

        [Column("TERMINAL_NAME")]
        public string? TerminalName { get; set; }

        [Column("PROCESS_ID")]
        public decimal? ProcessId { get; set; }

        [Column("PDLINE_ID")]
        public decimal? PdlineId { get; set; }

        [Column("STAGE_ID")]
        public decimal? StageId { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }

        [Column("MACHINE_ID")]
        public decimal? MachineId { get; set; }

        [Column("ON_LINE_PART_ID")]
        public decimal? OnLinePartId { get; set; }

        [Column("ON_LINE_TIME")]
        public DateTime? OnLineTime { get; set; }
    }

    [Table("sys_process")]
    public class SysProcess
    {
        [Key]
        [Column("PROCESS_ID")]
        public decimal ProcessId { get; set; }

        [Column("PROCESS_NAME")]
        public string? ProcessName { get; set; }

        [Column("PROCESS_CODE")]
        public string? ProcessCode { get; set; }

        [Column("PROCESS_DESC")]
        public string? ProcessDesc { get; set; }

        [Column("STAGE_ID")]
        public decimal? StageId { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }

        [Column("PROCESS_TYPE")]
        public string? ProcessType { get; set; }

        [Column("RETEST_COUNT")]
        public decimal? RetestCount { get; set; }
    }

    [Table("sys_route")]
    public class SysRoute
    {
        [Key]
        [Column("ROUTE_ID")]
        public decimal RouteId { get; set; }

        [Column("ROUTE_NAME")]
        public string? RouteName { get; set; }

        [Column("ROUTE_DESC")]
        public string? RouteDesc { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }

        [Column("FACTORY_ID")]
        public decimal? FactoryId { get; set; }
    }

    [Table("sys_stage")]
    public class SysStage
    {
        [Key]
        [Column("STAGE_ID")]
        public decimal StageId { get; set; }

        [Column("STAGE_NAME")]
        public string? StageName { get; set; }

        [Column("STAGE_CODE")]
        public string? StageCode { get; set; }

        [Column("STAGE_DESC")]
        public string? StageDesc { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }

        [Column("FACTORY_ID")]
        public decimal? FactoryId { get; set; }

        [Column("SIDE")]
        public decimal? Side { get; set; }

        [Column("OPERATION")]
        public string? Operation { get; set; }
    }

    [Table("sys_pdline")]
    public class SysPdline
    {
        [Key]
        [Column("PDLINE_ID")]
        public decimal PdlineId { get; set; }

        [Column("PDLINE_NAME")]
        public string? PdlineName { get; set; }

        [Column("PDLINE_DESC")]
        public string? PdlineDesc { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }

        [Column("FACTORY_ID")]
        public decimal? FactoryId { get; set; }

        [Column("STAGE_ID")]
        public decimal? StageId { get; set; }

        [Column("PDLINE_AREA")]
        public string? PdlineArea { get; set; }

        [Column("TYPE")]
        public string? Type { get; set; }
    }

    [Table("sys_factory")]
    public class SysFactory
    {
        [Key]
        [Column("FACTORY_ID")]
        public decimal FactoryId { get; set; }

        [Column("FACTORY_NAME")]
        public string? FactoryName { get; set; }

        [Column("FACTORY_DESC")]
        public string? FactoryDesc { get; set; }

        [Column("ENABLED")]
        public string? Enabled { get; set; }
    }

    // DTO สำหรับ Set Process
    public class SetProcessDto
    {
        public List<string> SerialNumbers { get; set; } = new List<string>();
        public decimal ProcessId { get; set; }
        public decimal? TerminalId { get; set; }
        public decimal? RouteId { get; set; }
        public decimal? StageId { get; set; }
        public decimal? PdlineId { get; set; }
        public string? Remarks { get; set; }
    }

    // DTO สำหรับ Response
    public class SetProcessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ProcessResult> Results { get; set; } = new List<ProcessResult>();
    }

    public class ProcessResult
    {
        public string SerialNumber { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO สำหรับ dropdown options
    public class ProcessOption
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
} 