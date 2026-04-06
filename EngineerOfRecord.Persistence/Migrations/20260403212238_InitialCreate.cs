using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineerOfRecord.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EngineerOfRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Discipline = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LicenseExpiration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LicensedStates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VantagepointEmployeeId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EmployeeFirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeLastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeePreferredName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VantagepointLastSynced = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineerOfRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EngineerOfRecord_VantagepointEmployeeId",
                table: "EngineerOfRecord",
                column: "VantagepointEmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EngineerOfRecord");
        }
    }
}
