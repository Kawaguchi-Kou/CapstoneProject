using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRiskscore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeatherRiskScore",
                table: "itinerary_details");

            migrationBuilder.AddColumn<double>(
                name: "PrecipitationProbability",
                table: "itinerary_details",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TemperatureCelsius",
                table: "itinerary_details",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WindSpeed",
                table: "itinerary_details",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecipitationProbability",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "TemperatureCelsius",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "WindSpeed",
                table: "itinerary_details");

            migrationBuilder.AddColumn<double>(
                name: "WeatherRiskScore",
                table: "itinerary_details",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
