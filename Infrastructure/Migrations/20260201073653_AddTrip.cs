using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvtUrl",
                table: "Accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EndLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferencesId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.TripId);
                });

            migrationBuilder.CreateTable(
                name: "itineraries",
                columns: table => new
                {
                    ItineraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itineraries", x => x.ItineraryId);
                    table.ForeignKey(
                        name: "FK_itineraries_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_segments",
                columns: table => new
                {
                    SegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false),
                    FromLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ToLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TravelDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DistanceKm = table.Column<float>(type: "real", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_segments", x => x.SegmentId);
                    table.ForeignKey(
                        name: "FK_trip_segments_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itinerary_details",
                columns: table => new
                {
                    DetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PoiId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeatherRiskScore = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SegmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerary_details", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_itinerary_details_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "ItineraryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itinerary_details_trip_segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "trip_segments",
                        principalColumn: "SegmentId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_TripId",
                table: "itineraries",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_details_ItineraryId",
                table: "itinerary_details",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_details_SegmentId",
                table: "itinerary_details",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_TripId_SequenceNo",
                table: "trip_segments",
                columns: new[] { "TripId", "SequenceNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itinerary_details");

            migrationBuilder.DropTable(
                name: "itineraries");

            migrationBuilder.DropTable(
                name: "trip_segments");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropColumn(
                name: "AvtUrl",
                table: "Accounts");
        }
    }
}
