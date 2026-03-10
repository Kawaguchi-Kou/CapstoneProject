using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SepayPaymentPendingSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ad_payments_account_subscriptions_SubscriptionId",
                table: "ad_payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "ad_payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "ad_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "ad_payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Accumulated",
                table: "ad_payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AmountIn",
                table: "ad_payments",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ad_payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gateway",
                table: "ad_payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                table: "ad_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SubAccount",
                table: "ad_payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionContent",
                table: "ad_payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDate",
                table: "ad_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAds",
                table: "account_subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "AdsUsed",
                table: "account_subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddForeignKey(
                name: "FK_ad_payments_account_subscriptions_SubscriptionId",
                table: "ad_payments",
                column: "SubscriptionId",
                principalTable: "account_subscriptions",
                principalColumn: "SubscriptionId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ad_payments_account_subscriptions_SubscriptionId",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "Accumulated",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "AmountIn",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "Gateway",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "SubAccount",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "TransactionContent",
                table: "ad_payments");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "ad_payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "ad_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "MaxAds",
                table: "account_subscriptions",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<float>(
                name: "AdsUsed",
                table: "account_subscriptions",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_ad_payments_account_subscriptions_SubscriptionId",
                table: "ad_payments",
                column: "SubscriptionId",
                principalTable: "account_subscriptions",
                principalColumn: "SubscriptionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
