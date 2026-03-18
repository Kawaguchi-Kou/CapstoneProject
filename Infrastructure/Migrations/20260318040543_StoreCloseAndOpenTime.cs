using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreCloseAndOpenTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "pois");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CloseHour",
                table: "pois",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Is24Hours",
                table: "pois",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpenHour",
                table: "pois",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitRecommendation",
                table: "pois",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseHour",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "Is24Hours",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "OpenHour",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "VisitRecommendation",
                table: "pois");

            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "pois",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
