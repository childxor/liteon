using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class adjustShift2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "break2_end_time",
                table: "emp_shift",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "break2_start_time",
                table: "emp_shift",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_break2",
                table: "emp_shift",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "break2_end_time",
                table: "emp_shift");

            migrationBuilder.DropColumn(
                name: "break2_start_time",
                table: "emp_shift");

            migrationBuilder.DropColumn(
                name: "is_break2",
                table: "emp_shift");
        }
    }
}
