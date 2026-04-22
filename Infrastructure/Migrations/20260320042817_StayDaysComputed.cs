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

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "trip_segments",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "trip_segments",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1));
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
