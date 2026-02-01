using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatigueMonitoring.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncMetadata", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncMetadata_SyncKey",
                table: "SyncMetadata",
                column: "SyncKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncMetadata");
        }
    }
}
