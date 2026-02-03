using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Advertisement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ForecastId",
                table: "pois",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ad_subscription_packages",
                columns: table => new
                {
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<float>(type: "real", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    MaxAdsPerPeriod = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ad_subscription_packages", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "WeatherForecast",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemperatureCelsius = table.Column<double>(type: "double precision", nullable: false),
                    WindSpeed = table.Column<double>(type: "double precision", nullable: false),
                    PrecipitationProbability = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecast", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxAds = table.Column<float>(type: "real", nullable: false),
                    AdsUsed = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_account_subscriptions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_subscriptions_ad_subscription_packages_Subscription~",
                        column: x => x.SubscriptionPackageId,
                        principalTable: "ad_subscription_packages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "advertisements",
                columns: table => new
                {
                    AdId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    POIId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VideoUrl = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertisements", x => x.AdId);
                    table.ForeignKey(
                        name: "FK_advertisements_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_advertisements_ad_subscription_packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "ad_subscription_packages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_advertisements_pois_POIId",
                        column: x => x.POIId,
                        principalTable: "pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_payments",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ad_payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_ad_payments_account_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "account_subscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedbacks",
                columns: table => new
                {
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeedbackType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedbacks", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_feedbacks_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_feedbacks_advertisements_AdId",
                        column: x => x.AdId,
                        principalTable: "advertisements",
                        principalColumn: "AdId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pois_ForecastId",
                table: "pois",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_account_subscriptions_AccountId",
                table: "account_subscriptions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_account_subscriptions_SubscriptionPackageId",
                table: "account_subscriptions",
                column: "SubscriptionPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ad_payments_SubscriptionId",
                table: "ad_payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_AccountId",
                table: "advertisements",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_PackageId",
                table: "advertisements",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_POIId",
                table: "advertisements",
                column: "POIId");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_AdId",
                table: "feedbacks",
                column: "AdId");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_UserId",
                table: "feedbacks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois",
                column: "ForecastId",
                principalTable: "WeatherForecast",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_WeatherForecast_ForecastId",
                table: "pois");

            migrationBuilder.DropTable(
                name: "ad_payments");

            migrationBuilder.DropTable(
                name: "feedbacks");

            migrationBuilder.DropTable(
                name: "WeatherForecast");

            migrationBuilder.DropTable(
                name: "account_subscriptions");

            migrationBuilder.DropTable(
                name: "advertisements");

            migrationBuilder.DropTable(
                name: "ad_subscription_packages");

            migrationBuilder.DropIndex(
                name: "IX_pois_ForecastId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "ForecastId",
                table: "pois");
        }
    }
}
