using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class addtable_emp_shift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emp_shift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    shift_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    shift_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    shift_group = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    start_duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    break_start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    break_end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    break_duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    work_3rd_start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    work_3rd_end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    work_3rd_duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sort = table.Column<int>(type: "int", nullable: false),
                    ot_start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    ot_end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    ot_duration = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_shift", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emp_shift");
        }
    }
}
