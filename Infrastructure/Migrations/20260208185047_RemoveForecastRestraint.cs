using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForecastRestraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois");

            migrationBuilder.AlterColumn<Guid>(
                name: "ForecastId",
                table: "pois",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois",
                column: "ForecastId",
                principalTable: "WeatherForecast",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois");

            migrationBuilder.AlterColumn<Guid>(
                name: "ForecastId",
                table: "pois",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois",
                column: "ForecastId",
                principalTable: "WeatherForecast",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
