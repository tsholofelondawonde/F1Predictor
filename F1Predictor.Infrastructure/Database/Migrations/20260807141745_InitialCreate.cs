using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Predictor.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "DriverRaceFeatures",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    DriverNumber = table.Column<int>(type: "integer", nullable: false),
                    GridPosition = table.Column<float>(type: "real", nullable: false),
                    QualiGapToPole = table.Column<float>(type: "real", nullable: false),
                    PitStopCount = table.Column<float>(type: "real", nullable: false),
                    AvgPitStopDuration = table.Column<float>(type: "real", nullable: false),
                    Rainfall = table.Column<float>(type: "real", nullable: false),
                    FinishPosition = table.Column<int>(type: "integer", nullable: false),
                    Podium = table.Column<bool>(type: "boolean", nullable: false),
                    PointsFinish = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverRaceFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                schema: "public",
                columns: table => new
                {
                    MeetingKey = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CircuitShortName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MeetingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.MeetingKey);
                });

            migrationBuilder.CreateTable(
                name: "PitStopEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    DriverNumber = table.Column<int>(type: "integer", nullable: false),
                    StopDuration = table.Column<double>(type: "double precision", nullable: true),
                    LaneDuration = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitStopEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaceSessions",
                schema: "public",
                columns: table => new
                {
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    MeetingKey = table.Column<int>(type: "integer", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QualifyingSessionKey = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceSessions", x => x.SessionKey);
                });

            migrationBuilder.CreateTable(
                name: "SessionResultEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    DriverNumber = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    Dnf = table.Column<bool>(type: "boolean", nullable: false),
                    Dns = table.Column<bool>(type: "boolean", nullable: false),
                    Dsq = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionResultEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StartingGridEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    DriverNumber = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    LapDuration = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartingGridEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherReadings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    Rainfall = table.Column<double>(type: "double precision", nullable: false),
                    TrackTemperature = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverRaceFeatures_SessionKey_DriverNumber",
                schema: "public",
                table: "DriverRaceFeatures",
                columns: new[] { "SessionKey", "DriverNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_Year",
                schema: "public",
                table: "Meetings",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_PitStopEntries_SessionKey_DriverNumber",
                schema: "public",
                table: "PitStopEntries",
                columns: new[] { "SessionKey", "DriverNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RaceSessions_DateStart",
                schema: "public",
                table: "RaceSessions",
                column: "DateStart");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSessions_MeetingKey",
                schema: "public",
                table: "RaceSessions",
                column: "MeetingKey");

            migrationBuilder.CreateIndex(
                name: "IX_SessionResultEntries_SessionKey_DriverNumber",
                schema: "public",
                table: "SessionResultEntries",
                columns: new[] { "SessionKey", "DriverNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_StartingGridEntries_SessionKey_DriverNumber",
                schema: "public",
                table: "StartingGridEntries",
                columns: new[] { "SessionKey", "DriverNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherReadings_SessionKey",
                schema: "public",
                table: "WeatherReadings",
                column: "SessionKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverRaceFeatures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Meetings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PitStopEntries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RaceSessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SessionResultEntries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StartingGridEntries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "WeatherReadings",
                schema: "public");
        }
    }
}
