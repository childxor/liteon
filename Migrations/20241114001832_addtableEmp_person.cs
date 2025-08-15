using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class addtableEmp_person : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emp_person",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    personID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isActive = table.Column<int>(type: "int", nullable: false),
                    RecordStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emp_person", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emp_person");
        }
    }
}
