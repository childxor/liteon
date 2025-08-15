using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class addcolshiftmentemp_person2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shiftMent",
                table: "emp_shift");

            migrationBuilder.AddColumn<string>(
                name: "shiftMent",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shiftMent",
                table: "emp_person");

            migrationBuilder.AddColumn<string>(
                name: "shiftMent",
                table: "emp_shift",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
