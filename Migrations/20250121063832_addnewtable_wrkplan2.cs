using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class addnewtable_wrkplan2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emp_wrkplan",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workplan_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    year = table.Column<int>(type: "int", nullable: true),
                    month = table.Column<int>(type: "int", nullable: true),
                    period = table.Column<int>(type: "int", nullable: true),
                    work_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    day_type = table.Column<string>(type: "nvarchar(1)", nullable: true),
                    shift_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cycle_day = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_wrkplan", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emp_wrkplan");
        }
    }
}
