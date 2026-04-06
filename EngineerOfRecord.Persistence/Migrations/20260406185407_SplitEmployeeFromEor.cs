using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineerOfRecord.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitEmployeeFromEor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeEmail",
                table: "EngineerOfRecord");

            migrationBuilder.DropColumn(
                name: "EmployeeFirstName",
                table: "EngineerOfRecord");

            migrationBuilder.DropColumn(
                name: "EmployeeLastName",
                table: "EngineerOfRecord");

            migrationBuilder.DropColumn(
                name: "EmployeePreferredName",
                table: "EngineerOfRecord");

            migrationBuilder.DropColumn(
                name: "EmployeeTitle",
                table: "EngineerOfRecord");

            migrationBuilder.DropColumn(
                name: "VantagepointLastSynced",
                table: "EngineerOfRecord");

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    VantagepointEmployeeId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreferredName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSynced = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.VantagepointEmployeeId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_EngineerOfRecord_Employee_VantagepointEmployeeId",
                table: "EngineerOfRecord",
                column: "VantagepointEmployeeId",
                principalTable: "Employee",
                principalColumn: "VantagepointEmployeeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EngineerOfRecord_Employee_VantagepointEmployeeId",
                table: "EngineerOfRecord");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeEmail",
                table: "EngineerOfRecord",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeFirstName",
                table: "EngineerOfRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeLastName",
                table: "EngineerOfRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeePreferredName",
                table: "EngineerOfRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeTitle",
                table: "EngineerOfRecord",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VantagepointLastSynced",
                table: "EngineerOfRecord",
                type: "datetime2",
                nullable: true);
        }
    }
}
