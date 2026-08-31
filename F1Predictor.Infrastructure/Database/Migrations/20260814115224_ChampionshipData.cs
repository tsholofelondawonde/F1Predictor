using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Predictor.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChampionshipData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Points",
                schema: "public",
                table: "SessionResultEntries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsClassified",
                schema: "public",
                table: "RaceSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSprint",
                schema: "public",
                table: "RaceSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DriverEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionKey = table.Column<int>(type: "integer", nullable: false),
                    DriverNumber = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameAcronym = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeamColour = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaceSessions_IsSprint_IsClassified",
                schema: "public",
                table: "RaceSessions",
                columns: new[] { "IsSprint", "IsClassified" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverEntries_SessionKey_DriverNumber",
                schema: "public",
                table: "DriverEntries",
                columns: new[] { "SessionKey", "DriverNumber" },
                unique: true);

            // Every row that already exists was written by the old ingest, which persisted a
            // session only once it had results and only ever matched session_name "Race". So
            // existing rows are classified Grands Prix by construction, and defaulting
            // IsClassified to false would hide the entire back catalogue from training and
            // standings until someone happened to re-ingest with force.
            migrationBuilder.Sql("""UPDATE public."RaceSessions" SET "IsClassified" = TRUE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverEntries",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_RaceSessions_IsSprint_IsClassified",
                schema: "public",
                table: "RaceSessions");

            migrationBuilder.DropColumn(
                name: "Points",
                schema: "public",
                table: "SessionResultEntries");

            migrationBuilder.DropColumn(
                name: "IsClassified",
                schema: "public",
                table: "RaceSessions");

            migrationBuilder.DropColumn(
                name: "IsSprint",
                schema: "public",
                table: "RaceSessions");
        }
    }
}
