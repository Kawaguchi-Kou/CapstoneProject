using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MinorChangeInRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_POIPreferences_Preferences_PreferenceId",
                table: "POIPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_POIPreferences_pois_PoiId",
                table: "POIPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_Locations_LocationId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipients_accounts_RecipientId",
                table: "Recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipients_notifications_NotificationId",
                table: "Recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_trip_segments_Locations_LocationId",
                table: "trip_segments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_Preferences_PreferenceId",
                table: "UserPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_accounts_AccountId",
                table: "UserPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherForecast_Locations_LocationId",
                table: "WeatherForecast");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recipients",
                table: "Recipients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Locations",
                table: "Locations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherForecast",
                table: "WeatherForecast");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPreferences",
                table: "UserPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_POIPreferences",
                table: "POIPreferences");

            migrationBuilder.RenameTable(
                name: "Recipients",
                newName: "recipients");

            migrationBuilder.RenameTable(
                name: "Locations",
                newName: "locations");

            migrationBuilder.RenameTable(
                name: "WeatherForecast",
                newName: "weather_forecast");

            migrationBuilder.RenameTable(
                name: "UserPreferences",
                newName: "user_preferences");

            migrationBuilder.RenameTable(
                name: "POIPreferences",
                newName: "poi_preferences");

            migrationBuilder.RenameIndex(
                name: "IX_Recipients_RecipientId",
                table: "recipients",
                newName: "IX_recipients_RecipientId");

            migrationBuilder.RenameIndex(
                name: "IX_Recipients_NotificationId",
                table: "recipients",
                newName: "IX_recipients_NotificationId");

            migrationBuilder.RenameIndex(
                name: "IX_WeatherForecast_LocationId",
                table: "weather_forecast",
                newName: "IX_weather_forecast_LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_WeatherForecast_City",
                table: "weather_forecast",
                newName: "IX_weather_forecast_City");

            migrationBuilder.RenameIndex(
                name: "IX_UserPreferences_PreferenceId",
                table: "user_preferences",
                newName: "IX_user_preferences_PreferenceId");

            migrationBuilder.RenameIndex(
                name: "IX_UserPreferences_AccountId",
                table: "user_preferences",
                newName: "IX_user_preferences_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_POIPreferences_PreferenceId",
                table: "poi_preferences",
                newName: "IX_poi_preferences_PreferenceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_recipients",
                table: "recipients",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_locations",
                table: "locations",
                column: "LocationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_weather_forecast",
                table: "weather_forecast",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_preferences",
                table: "user_preferences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_poi_preferences",
                table: "poi_preferences",
                columns: new[] { "PoiId", "PreferenceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_poi_preferences_Preferences_PreferenceId",
                table: "poi_preferences",
                column: "PreferenceId",
                principalTable: "Preferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poi_preferences_pois_PoiId",
                table: "poi_preferences",
                column: "PoiId",
                principalTable: "pois",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recipients_accounts_RecipientId",
                table: "recipients",
                column: "RecipientId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recipients_notifications_NotificationId",
                table: "recipients",
                column: "NotificationId",
                principalTable: "notifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trip_segments_locations_LocationId",
                table: "trip_segments",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_preferences_Preferences_PreferenceId",
                table: "user_preferences",
                column: "PreferenceId",
                principalTable: "Preferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_preferences_accounts_AccountId",
                table: "user_preferences",
                column: "AccountId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_weather_forecast_locations_LocationId",
                table: "weather_forecast",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_poi_preferences_Preferences_PreferenceId",
                table: "poi_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_poi_preferences_pois_PoiId",
                table: "poi_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_recipients_accounts_RecipientId",
                table: "recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_recipients_notifications_NotificationId",
                table: "recipients");

            migrationBuilder.DropForeignKey(
                name: "FK_trip_segments_locations_LocationId",
                table: "trip_segments");

            migrationBuilder.DropForeignKey(
                name: "FK_user_preferences_Preferences_PreferenceId",
                table: "user_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_user_preferences_accounts_AccountId",
                table: "user_preferences");

            migrationBuilder.DropForeignKey(
                name: "FK_weather_forecast_locations_LocationId",
                table: "weather_forecast");

            migrationBuilder.DropPrimaryKey(
                name: "PK_recipients",
                table: "recipients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_locations",
                table: "locations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_weather_forecast",
                table: "weather_forecast");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_preferences",
                table: "user_preferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_poi_preferences",
                table: "poi_preferences");

            migrationBuilder.RenameTable(
                name: "recipients",
                newName: "Recipients");

            migrationBuilder.RenameTable(
                name: "locations",
                newName: "Locations");

            migrationBuilder.RenameTable(
                name: "weather_forecast",
                newName: "WeatherForecast");

            migrationBuilder.RenameTable(
                name: "user_preferences",
                newName: "UserPreferences");

            migrationBuilder.RenameTable(
                name: "poi_preferences",
                newName: "POIPreferences");

            migrationBuilder.RenameIndex(
                name: "IX_recipients_RecipientId",
                table: "Recipients",
                newName: "IX_Recipients_RecipientId");

            migrationBuilder.RenameIndex(
                name: "IX_recipients_NotificationId",
                table: "Recipients",
                newName: "IX_Recipients_NotificationId");

            migrationBuilder.RenameIndex(
                name: "IX_weather_forecast_LocationId",
                table: "WeatherForecast",
                newName: "IX_WeatherForecast_LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_weather_forecast_City",
                table: "WeatherForecast",
                newName: "IX_WeatherForecast_City");

            migrationBuilder.RenameIndex(
                name: "IX_user_preferences_PreferenceId",
                table: "UserPreferences",
                newName: "IX_UserPreferences_PreferenceId");

            migrationBuilder.RenameIndex(
                name: "IX_user_preferences_AccountId",
                table: "UserPreferences",
                newName: "IX_UserPreferences_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_poi_preferences_PreferenceId",
                table: "POIPreferences",
                newName: "IX_POIPreferences_PreferenceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recipients",
                table: "Recipients",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Locations",
                table: "Locations",
                column: "LocationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherForecast",
                table: "WeatherForecast",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPreferences",
                table: "UserPreferences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_POIPreferences",
                table: "POIPreferences",
                columns: new[] { "PoiId", "PreferenceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_POIPreferences_Preferences_PreferenceId",
                table: "POIPreferences",
                column: "PreferenceId",
                principalTable: "Preferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_POIPreferences_pois_PoiId",
                table: "POIPreferences",
                column: "PoiId",
                principalTable: "pois",
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
                name: "FK_Recipients_notifications_NotificationId",
                table: "Recipients",
                column: "NotificationId",
                principalTable: "notifications",
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
                name: "FK_UserPreferences_Preferences_PreferenceId",
                table: "UserPreferences",
                column: "PreferenceId",
                principalTable: "Preferences",
                principalColumn: "Id",
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
    }
}
