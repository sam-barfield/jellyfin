using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class AddItemDubbedSubbedCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemDubbedCount",
                table: "BaseItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemSubbedCount",
                table: "BaseItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "ItemDubbedCount", "ItemSubbedCount" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemDubbedCount",
                table: "BaseItems");

            migrationBuilder.DropColumn(
                name: "ItemSubbedCount",
                table: "BaseItems");
        }
    }
}
