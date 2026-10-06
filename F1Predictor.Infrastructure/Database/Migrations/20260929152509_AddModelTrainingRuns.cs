using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Predictor.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddModelTrainingRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelTrainingRuns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Target = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FromYear = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TrainerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FeatureNames = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TrainingRaceCount = table.Column<int>(type: "integer", nullable: false),
                    TrainingRowCount = table.Column<int>(type: "integer", nullable: false),
                    ValidationRaceCount = table.Column<int>(type: "integer", nullable: false),
                    ValidationRowCount = table.Column<int>(type: "integer", nullable: false),
                    ValidationAuc = table.Column<double>(type: "double precision", nullable: false),
                    ValidationF1 = table.Column<double>(type: "double precision", nullable: false),
                    ValidationLogLoss = table.Column<double>(type: "double precision", nullable: false),
                    ValidationAuprc = table.Column<double>(type: "double precision", nullable: false),
                    ValidationPrecision = table.Column<double>(type: "double precision", nullable: false),
                    ValidationRecall = table.Column<double>(type: "double precision", nullable: false),
                    BaselineAuc = table.Column<double>(type: "double precision", nullable: true),
                    HoldoutSessionKey = table.Column<int>(type: "integer", nullable: false),
                    HoldoutAuc = table.Column<double>(type: "double precision", nullable: true),
                    HoldoutLogLoss = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelTrainingRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingRuns_Target_TrainedAt",
                schema: "public",
                table: "ModelTrainingRuns",
                columns: new[] { "Target", "TrainedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelTrainingRuns",
                schema: "public");
        }
    }
}
