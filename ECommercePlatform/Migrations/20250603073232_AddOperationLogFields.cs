using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommercePlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationLogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 7, 32, 31, 967, DateTimeKind.Utc).AddTicks(8557));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 7, 32, 31, 967, DateTimeKind.Utc).AddTicks(9438));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 6, 40, 38, 238, DateTimeKind.Utc).AddTicks(1300));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 6, 40, 38, 238, DateTimeKind.Utc).AddTicks(2078));
        }
    }
}
