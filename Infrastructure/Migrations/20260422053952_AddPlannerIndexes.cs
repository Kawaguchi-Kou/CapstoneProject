using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannerIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================
            // DROP OLD INDEXES
            // =========================
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_weather_forecast_City_ForecastDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_weather_forecast_LocationId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_trip_segments_TripId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_pois_LocationId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_districts_LocationId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_weather_forecast_LocationId_ForecastDate"";");

            // =========================
            // 1. ADD COLUMN (NULLABLE)
            // =========================
            migrationBuilder.AddColumn<Guid>(
            name: "DistrictId",
            table: "trip_segments",
            type: "uuid",
            nullable: true);

            // =========================
            // 2. BACKFILL DATA (CRITICAL)
            // =========================
            migrationBuilder.Sql(@"
        UPDATE trip_segments ts
        SET ""DistrictId"" = d.""Id""
        FROM districts d
        WHERE d.""LocationId"" = ts.""LocationId"";
    ");

            migrationBuilder.Sql(@"
UPDATE trip_segments ts
SET ""DistrictId"" = d.""Id""
FROM districts d
WHERE d.""LocationId"" = ts.""LocationId""
AND ts.""DistrictId"" IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE trip_segments
SET ""DistrictId"" = (
    SELECT ""Id""
    FROM districts
    LIMIT 1
)
WHERE ""DistrictId"" IS NULL;
");

            // =========================
            // 3. MAKE NOT NULL
            // =========================
            migrationBuilder.AlterColumn<Guid>(
        name: "DistrictId",
        table: "trip_segments",
        type: "uuid",
        nullable: false,
        oldClrType: typeof(Guid),
        oldType: "uuid",
        oldNullable: true);

            // =========================
            // INDEXES
            // =========================
            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_LocationId_ForecastDate",
                table: "weather_forecast",
                columns: new[] { "LocationId", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_DistrictId_OrderIndex",
                table: "trip_segments",
                columns: new[] { "DistrictId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_TripId_OrderIndex",
                table: "trip_segments",
                columns: new[] { "TripId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_pois_LocationId_DistrictId",
                table: "pois",
                columns: new[] { "LocationId", "DistrictId" });

            migrationBuilder.CreateIndex(
                name: "IX_districts_LocationId_Name",
                table: "districts",
                columns: new[] { "LocationId", "Name" });

            // =========================
            // FK (AFTER DATA FIX)
            // =========================
            migrationBuilder.AddForeignKey(
                name: "FK_trip_segments_districts_DistrictId",
                table: "trip_segments",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trip_segments_districts_DistrictId",
                table: "trip_segments");

            migrationBuilder.DropIndex(
                name: "IX_weather_forecast_LocationId_ForecastDate",
                table: "weather_forecast");

            migrationBuilder.DropIndex(
                name: "IX_trip_segments_DistrictId_OrderIndex",
                table: "trip_segments");

            migrationBuilder.DropIndex(
                name: "IX_trip_segments_TripId_OrderIndex",
                table: "trip_segments");

            migrationBuilder.DropIndex(
                name: "IX_pois_LocationId_DistrictId",
                table: "pois");

            migrationBuilder.DropIndex(
                name: "IX_districts_LocationId_Name",
                table: "districts");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "trip_segments");

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_City_ForecastDate",
                table: "weather_forecast",
                columns: new[] { "City", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecast_LocationId",
                table: "weather_forecast",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_TripId",
                table: "trip_segments",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_pois_LocationId",
                table: "pois",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_districts_LocationId",
                table: "districts",
                column: "LocationId");
        }
    }
}
