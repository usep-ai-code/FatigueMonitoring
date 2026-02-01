using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatigueMonitoring.Web.Api.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AI_ActiveAlerts_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AlarmId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Unit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                OpenMinutes = table.Column<int>(type: "int", nullable: false),
                SpeedKph = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                AlarmType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Latitude = table.Column<double>(type: "float", nullable: true),
                Longitude = table.Column<double>(type: "float", nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                EventCount = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_ActiveAlerts_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AI_AreaDistribution_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                OpenCount = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_AreaDistribution_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AI_DelayedAlerts_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AlarmId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Unit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                OpenMinutes = table.Column<int>(type: "int", nullable: false),
                SpeedKph = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                AlarmType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Latitude = table.Column<double>(type: "float", nullable: true),
                Longitude = table.Column<double>(type: "float", nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                EventCount = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_DelayedAlerts_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AI_DeviceHealth_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TotalDevices = table.Column<int>(type: "int", nullable: false),
                OnlineDevices = table.Column<int>(type: "int", nullable: false),
                OfflineDevices = table.Column<int>(type: "int", nullable: false),
                Coverage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_DeviceHealth_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AI_Kpi_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                TotalAlarms = table.Column<int>(type: "int", nullable: false),
                FollowedUp = table.Column<int>(type: "int", nullable: false),
                WaitingFollowUp = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_Kpi_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AI_RecurrentUnits_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Unit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                EventsCount = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_RecurrentUnits_T", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RawEvents",
            columns: table => new
            {
                ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Identity = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                AlarmType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                DeviceTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                ServerTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                Shift = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Level = table.Column<int>(type: "int", nullable: true),
                SpeedKph = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                IsFollowedUp = table.Column<bool>(type: "bit", nullable: false),
                Latitude = table.Column<double>(type: "float", nullable: true),
                Longitude = table.Column<double>(type: "float", nullable: true),
                GeofenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DeviceName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                DeviceGroupName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ManualVerificationBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ManualVerificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                ManualVerificationMemo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ManualVerificationWaitingDuration = table.Column<int>(type: "int", nullable: true),
                UploadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                LocationLabel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                OperatorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RawEvents", x => x.ExternalId);
            });

        migrationBuilder.CreateTable(
            name: "AI_HighRiskAreas_T",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Area = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                EventsCount = table.Column<int>(type: "int", nullable: false),
                SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AI_HighRiskAreas_T", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AI_ActiveAlerts_T_Area",
            table: "AI_ActiveAlerts_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_AI_AreaDistribution_T_Area",
            table: "AI_AreaDistribution_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_AI_DelayedAlerts_T_Area",
            table: "AI_DelayedAlerts_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_AI_HighRiskAreas_T_Area",
            table: "AI_HighRiskAreas_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_AI_Kpi_T_Area",
            table: "AI_Kpi_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_AI_RecurrentUnits_T_Area",
            table: "AI_RecurrentUnits_T",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_RawEvents_Area",
            table: "RawEvents",
            column: "Area");

        migrationBuilder.CreateIndex(
            name: "IX_RawEvents_DeviceTime",
            table: "RawEvents",
            column: "DeviceTime");

        migrationBuilder.CreateIndex(
            name: "IX_RawEvents_IsFollowedUp",
            table: "RawEvents",
            column: "IsFollowedUp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AI_ActiveAlerts_T");
        migrationBuilder.DropTable(name: "AI_AreaDistribution_T");
        migrationBuilder.DropTable(name: "AI_DelayedAlerts_T");
        migrationBuilder.DropTable(name: "AI_DeviceHealth_T");
        migrationBuilder.DropTable(name: "AI_HighRiskAreas_T");
        migrationBuilder.DropTable(name: "AI_Kpi_T");
        migrationBuilder.DropTable(name: "AI_RecurrentUnits_T");
        migrationBuilder.DropTable(name: "RawEvents");
    }
}
