using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StayDaysComputed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StayDays",
                table: "trip_segments");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "trip_segments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "trip_segments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "trip_segments");

            migrationBuilder.AddColumn<int>(
                name: "StayDays",
                table: "trip_segments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
