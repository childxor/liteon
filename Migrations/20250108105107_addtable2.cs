using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class addtable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emp_person_shift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    shiftMent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    personID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    deptCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    deptName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    deptID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isActive = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_person_shift", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emp_person_shift");
        }
    }
}
