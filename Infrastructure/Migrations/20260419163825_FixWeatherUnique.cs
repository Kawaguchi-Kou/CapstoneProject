using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWeatherUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_weather_forecast_City_ForecastDate",
                table: "weather_forecast");

            migrationBuilder.DropIndex(
                name: "IX_weather_forecast_LocationId",
                table: "weather_forecast");

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_LocationId_ForecastDate",
                table: "weather_forecast",
                columns: new[] { "LocationId", "ForecastDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_weather_forecast_LocationId_ForecastDate",
                table: "weather_forecast");

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_City_ForecastDate",
                table: "weather_forecast",
                columns: new[] { "City", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_LocationId",
                table: "weather_forecast",
                column: "LocationId");
        }
    }
}
