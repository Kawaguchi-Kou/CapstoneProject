using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_account_subscriptions_Accounts_AccountId",
                table: "account_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Roles_RoleId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_advertisements_Accounts_AccountId",
                table: "advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_itineraries_trips_TripId",
                table: "itineraries");

            migrationBuilder.DropForeignKey(
                name: "FK_itinerary_details_trip_segments_SegmentId",
                table: "itinerary_details");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_Accounts_SenderId",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipients_Accounts_RecipientId",
                table: "Recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Accounts_AccountId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_Accounts_AccountId",
                table: "UserPreferences");

            migrationBuilder.DropTable(
                name: "manual_overrides");

            migrationBuilder.DropIndex(
                name: "IX_trip_segments_TripId_SequenceNo",
                table: "trip_segments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_pois_ForecastId",
                table: "pois");

            migrationBuilder.DropIndex(
                name: "IX_itinerary_details_SegmentId",
                table: "itinerary_details");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OtpVerifications",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "EndLocation",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "TravelDate",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "ForecastId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "SegmentId",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "itineraries");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "Accounts",
                newName: "accounts");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "refresh_tokens");

            migrationBuilder.RenameTable(
                name: "OtpVerifications",
                newName: "otp_verifications");

            migrationBuilder.RenameColumn(
                name: "StartLocation",
                table: "trips",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "SequenceNo",
                table: "trip_segments",
                newName: "StayDays");

            migrationBuilder.RenameColumn(
                name: "SegmentType",
                table: "trip_segments",
                newName: "OrderIndex");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "pois",
                newName: "OpeningHours");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "itinerary_details",
                newName: "VisitDate");

            migrationBuilder.RenameIndex(
                name: "IX_Accounts_RoleId",
                table: "accounts",
                newName: "IX_accounts_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Accounts_Email",
                table: "accounts",
                newName: "IX_accounts_Email");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_AccountId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_AccountId");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "WeatherForecast",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TripType",
                table: "trips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "trip_segments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "GoogleMapLink",
                table: "pois",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsIndoor",
                table: "pois",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "pois",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "itinerary_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "itinerary_details",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                table: "itinerary_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RiskCalculatedAt",
                table: "itinerary_details",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "itinerary_details",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "itineraries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "GeneratedByAI",
                table: "itineraries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SegmentId",
                table: "itineraries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "itineraries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "itineraries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounts",
                table: "accounts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_otp_verifications",
                table: "otp_verifications",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationId);
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "LocationId", "LocationName", "Latitude", "Longitude" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000000"),
                    "Unknown",
                    0m,
                    0m
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecast_LocationId",
                table: "WeatherForecast",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_LocationId",
                table: "trip_segments",
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
                name: "IX_itinerary_details_PoiId",
                table: "itinerary_details",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_SegmentId",
                table: "itineraries",
                column: "SegmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_account_subscriptions_accounts_AccountId",
                table: "account_subscriptions",
                column: "AccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_roles_RoleId",
                table: "accounts",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_advertisements_accounts_AccountId",
                table: "advertisements",
                column: "AccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_itineraries_trip_segments_SegmentId",
                table: "itineraries",
                column: "SegmentId",
                principalTable: "trip_segments",
                principalColumn: "SegmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_itineraries_trips_TripId",
                table: "itineraries",
                column: "TripId",
                principalTable: "trips",
                principalColumn: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_itinerary_details_pois_PoiId",
                table: "itinerary_details",
                column: "PoiId",
                principalTable: "pois",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_accounts_SenderId",
                table: "notifications",
                column: "SenderId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_Locations_LocationId",
                table: "pois",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipients_accounts_RecipientId",
                table: "Recipients",
                column: "RecipientId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_accounts_AccountId",
                table: "refresh_tokens",
                column: "AccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trip_segments_Locations_LocationId",
                table: "trip_segments",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_accounts_AccountId",
                table: "UserPreferences",
                column: "AccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherForecast_Locations_LocationId",
                table: "WeatherForecast",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_account_subscriptions_accounts_AccountId",
                table: "account_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_accounts_roles_RoleId",
                table: "accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_advertisements_accounts_AccountId",
                table: "advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_itineraries_trip_segments_SegmentId",
                table: "itineraries");

            migrationBuilder.DropForeignKey(
                name: "FK_itineraries_trips_TripId",
                table: "itineraries");

            migrationBuilder.DropForeignKey(
                name: "FK_itinerary_details_pois_PoiId",
                table: "itinerary_details");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_accounts_SenderId",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_Locations_LocationId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipients_accounts_RecipientId",
                table: "Recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_accounts_AccountId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_trip_segments_Locations_LocationId",
                table: "trip_segments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_accounts_AccountId",
                table: "UserPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherForecast_Locations_LocationId",
                table: "WeatherForecast");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_WeatherForecast_LocationId",
                table: "WeatherForecast");

            migrationBuilder.DropIndex(
                name: "IX_trip_segments_LocationId",
                table: "trip_segments");

            migrationBuilder.DropIndex(
                name: "IX_trip_segments_TripId",
                table: "trip_segments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_pois_LocationId",
                table: "pois");

            migrationBuilder.DropIndex(
                name: "IX_itinerary_details_PoiId",
                table: "itinerary_details");

            migrationBuilder.DropIndex(
                name: "IX_itineraries_SegmentId",
                table: "itineraries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounts",
                table: "accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_otp_verifications",
                table: "otp_verifications");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "WeatherForecast");

            migrationBuilder.DropColumn(
                name: "TripType",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "trip_segments");

            migrationBuilder.DropColumn(
                name: "GoogleMapLink",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "IsIndoor",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "RiskCalculatedAt",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "itinerary_details");

            migrationBuilder.DropColumn(
                name: "GeneratedByAI",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "SegmentId",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "itineraries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "itineraries");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "accounts",
                newName: "Accounts");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "otp_verifications",
                newName: "OtpVerifications");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "trips",
                newName: "StartLocation");

            migrationBuilder.RenameColumn(
                name: "StayDays",
                table: "trip_segments",
                newName: "SequenceNo");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "trip_segments",
                newName: "SegmentType");

            migrationBuilder.RenameColumn(
                name: "OpeningHours",
                table: "pois",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "VisitDate",
                table: "itinerary_details",
                newName: "Date");

            migrationBuilder.RenameIndex(
                name: "IX_accounts_RoleId",
                table: "Accounts",
                newName: "IX_Accounts_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_accounts_Email",
                table: "Accounts",
                newName: "IX_Accounts_Email");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_AccountId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_AccountId");

            migrationBuilder.AddColumn<string>(
                name: "EndLocation",
                table: "trips",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "trip_segments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "trip_segments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                table: "trip_segments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TravelDate",
                table: "trip_segments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ForecastId",
                table: "pois",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SegmentId",
                table: "itinerary_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "itineraries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "itineraries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "itineraries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OtpVerifications",
                table: "OtpVerifications",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "manual_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UserConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    WarningShown = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manual_overrides_itinerary_details_DetailId",
                        column: x => x.DetailId,
                        principalTable: "itinerary_details",
                        principalColumn: "DetailId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trip_segments_TripId_SequenceNo",
                table: "trip_segments",
                columns: new[] { "TripId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pois_ForecastId",
                table: "pois",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_details_SegmentId",
                table: "itinerary_details",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_manual_overrides_DetailId",
                table: "manual_overrides",
                column: "DetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_account_subscriptions_Accounts_AccountId",
                table: "account_subscriptions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Roles_RoleId",
                table: "Accounts",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_advertisements_Accounts_AccountId",
                table: "advertisements",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_itineraries_trips_TripId",
                table: "itineraries",
                column: "TripId",
                principalTable: "trips",
                principalColumn: "TripId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_itinerary_details_trip_segments_SegmentId",
                table: "itinerary_details",
                column: "SegmentId",
                principalTable: "trip_segments",
                principalColumn: "SegmentId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_Accounts_SenderId",
                table: "notifications",
                column: "SenderId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois",
                column: "ForecastId",
                principalTable: "WeatherForecast",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipients_Accounts_RecipientId",
                table: "Recipients",
                column: "RecipientId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Accounts_AccountId",
                table: "RefreshTokens",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_Accounts_AccountId",
                table: "UserPreferences",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
