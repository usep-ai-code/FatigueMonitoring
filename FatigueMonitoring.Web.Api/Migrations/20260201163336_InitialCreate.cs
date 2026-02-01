using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatigueMonitoring.Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AI_ActiveAlert_T",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    OpenDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_ActiveAlert_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_AreaDistribution_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlertCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_AreaDistribution_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_DashboardStats_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalAlarms = table.Column<int>(type: "int", nullable: false),
                    FollowedUp = table.Column<int>(type: "int", nullable: false),
                    WaitingFollowUp = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_DashboardStats_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_HighRiskArea_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_HighRiskArea_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_RecurrentUnit_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_RecurrentUnit_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Identity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlarmType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServerTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    IsFollowedUp = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    GeofenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManualVerificationBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManualVerificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManualVerificationMemo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TakeType = table.Column<bool>(type: "bit", nullable: false),
                    ManualVerificationWaitingDuration = table.Column<int>(type: "int", nullable: false),
                    Satellites = table.Column<int>(type: "int", nullable: false),
                    UploadAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceImei = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeofenceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawFollowUps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlarmId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpCategoryId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpCategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupervisorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawFollowUps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_ActiveAlert_T_Area",
                table: "AI_ActiveAlert_T",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_AI_ActiveAlert_T_Status",
                table: "AI_ActiveAlert_T",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DashboardStats_T_Area",
                table: "AI_DashboardStats_T",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_RawEvents_DeviceId",
                table: "RawEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RawEvents_IsFollowedUp",
                table: "RawEvents",
                column: "IsFollowedUp");

            migrationBuilder.CreateIndex(
                name: "IX_RawEvents_Time",
                table: "RawEvents",
                column: "Time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_ActiveAlert_T");

            migrationBuilder.DropTable(
                name: "AI_AreaDistribution_T");

            migrationBuilder.DropTable(
                name: "AI_DashboardStats_T");

            migrationBuilder.DropTable(
                name: "AI_HighRiskArea_T");

            migrationBuilder.DropTable(
                name: "AI_RecurrentUnit_T");

            migrationBuilder.DropTable(
                name: "RawEvents");

            migrationBuilder.DropTable(
                name: "RawFollowUps");
        }
    }
}
