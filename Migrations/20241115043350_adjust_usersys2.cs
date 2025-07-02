using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_TH.Migrations
{
    /// <inheritdoc />
    public partial class adjust_usersys2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "personID",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CREATEDATE",
                table: "emp_person",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cardCategory",
                table: "emp_person",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cardNumber",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cardStatus",
                table: "emp_person",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cardType",
                table: "emp_person",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cardTypeDesc",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deptID",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deptName",
                table: "emp_person",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "disableDate",
                table: "emp_person",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "enableDate",
                table: "emp_person",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "leaveJobDate",
                table: "emp_person",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "modifyTime",
                table: "emp_person",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "emp_person",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reserve1",
                table: "emp_person",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reserve2",
                table: "emp_person",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reserve3",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reserve4",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reserveChar1",
                table: "emp_person",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subSystem",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "superPassword",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "useCategory",
                table: "emp_person",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "useStatus",
                table: "emp_person",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "userLevel",
                table: "emp_person",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CREATEDATE",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "cardCategory",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "cardNumber",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "cardStatus",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "cardType",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "cardTypeDesc",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "deptID",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "deptName",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "disableDate",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "enableDate",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "leaveJobDate",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "modifyTime",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "name",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "password",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "reserve1",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "reserve2",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "reserve3",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "reserve4",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "reserveChar1",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "status",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "subSystem",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "superPassword",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "useCategory",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "useStatus",
                table: "emp_person");

            migrationBuilder.DropColumn(
                name: "userLevel",
                table: "emp_person");

            migrationBuilder.AlterColumn<string>(
                name: "personID",
                table: "emp_person",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
