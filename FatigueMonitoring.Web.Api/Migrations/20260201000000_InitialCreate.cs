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
                name: "AI_FatigueEvent_T",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Identity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlarmName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlarmType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServerTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsFollowedUp = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: false),
                    GeofenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManualVerificationBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManualVerificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManualVerificationMemo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManualVerificationWaitingDuration = table.Column<int>(type: "int", nullable: true),
                    DeviceImei = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_FatigueEvent_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_DashboardSummary_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilterType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalAlarms = table.Column<int>(type: "int", nullable: false),
                    FollowedUp = table.Column<int>(type: "int", nullable: false),
                    WaitingFollowUp = table.Column<int>(type: "int", nullable: false),
                    MiningTotal = table.Column<int>(type: "int", nullable: false),
                    MiningOpen = table.Column<int>(type: "int", nullable: false),
                    MiningResolved = table.Column<int>(type: "int", nullable: false),
                    HaulingTotal = table.Column<int>(type: "int", nullable: false),
                    HaulingOpen = table.Column<int>(type: "int", nullable: false),
                    HaulingResolved = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_DashboardSummary_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_AreaDistribution_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpenAlertCount = table.Column<int>(type: "int", nullable: false),
                    TotalAlertCount = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_AreaDistribution_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_ActiveAlert_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FatigueEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Speed = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    AlertCountToday = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_ActiveAlert_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_DelayedFollowUp_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FatigueEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DelayMinutes = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_DelayedFollowUp_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_RecurrentUnit_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    PrimaryArea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_RecurrentUnit_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_HighRiskArea_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Location = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_HighRiskArea_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_TokenCache_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_TokenCache_T", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AI_SyncState_T",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncRecordCount = table.Column<int>(type: "int", nullable: false),
                    LastSyncStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSyncError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_SyncState_T", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_AI_FatigueEvent_T_ExternalId",
                table: "AI_FatigueEvent_T",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AI_FatigueEvent_T_EventTime",
                table: "AI_FatigueEvent_T",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_AI_FatigueEvent_T_IsFollowedUp",
                table: "AI_FatigueEvent_T",
                column: "IsFollowedUp");

            migrationBuilder.CreateIndex(
                name: "IX_AI_FatigueEvent_T_Area",
                table: "AI_FatigueEvent_T",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DashboardSummary_T_FilterType",
                table: "AI_DashboardSummary_T",
                column: "FilterType");

            migrationBuilder.CreateIndex(
                name: "IX_AI_AreaDistribution_T_Area_Location",
                table: "AI_AreaDistribution_T",
                columns: new[] { "Area", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_AI_ActiveAlert_T_FatigueEventId",
                table: "AI_ActiveAlert_T",
                column: "FatigueEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AI_ActiveAlert_T_Status",
                table: "AI_ActiveAlert_T",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AI_ActiveAlert_T_Area",
                table: "AI_ActiveAlert_T",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DelayedFollowUp_T_FatigueEventId",
                table: "AI_DelayedFollowUp_T",
                column: "FatigueEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AI_DelayedFollowUp_T_DelayMinutes",
                table: "AI_DelayedFollowUp_T",
                column: "DelayMinutes");

            migrationBuilder.CreateIndex(
                name: "IX_AI_RecurrentUnit_T_UnitName",
                table: "AI_RecurrentUnit_T",
                column: "UnitName");

            migrationBuilder.CreateIndex(
                name: "IX_AI_RecurrentUnit_T_EventCount",
                table: "AI_RecurrentUnit_T",
                column: "EventCount");

            migrationBuilder.CreateIndex(
                name: "IX_AI_HighRiskArea_T_Location",
                table: "AI_HighRiskArea_T",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_AI_HighRiskArea_T_EventCount",
                table: "AI_HighRiskArea_T",
                column: "EventCount");

            migrationBuilder.CreateIndex(
                name: "IX_AI_TokenCache_T_TokenType",
                table: "AI_TokenCache_T",
                column: "TokenType");

            migrationBuilder.CreateIndex(
                name: "IX_AI_SyncState_T_SyncType",
                table: "AI_SyncState_T",
                column: "SyncType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AI_SyncState_T");
            migrationBuilder.DropTable(name: "AI_TokenCache_T");
            migrationBuilder.DropTable(name: "AI_HighRiskArea_T");
            migrationBuilder.DropTable(name: "AI_RecurrentUnit_T");
            migrationBuilder.DropTable(name: "AI_DelayedFollowUp_T");
            migrationBuilder.DropTable(name: "AI_ActiveAlert_T");
            migrationBuilder.DropTable(name: "AI_AreaDistribution_T");
            migrationBuilder.DropTable(name: "AI_DashboardSummary_T");
            migrationBuilder.DropTable(name: "AI_FatigueEvent_T");
        }
    }
}
