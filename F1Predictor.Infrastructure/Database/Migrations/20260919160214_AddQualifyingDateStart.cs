using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Predictor.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddQualifyingDateStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QualifyingDateStart",
                schema: "public",
                table: "RaceSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QualifyingDateStart",
                schema: "public",
                table: "RaceSessions");
        }
    }
}
