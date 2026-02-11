using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditTypeInAccSubAndAdSubPac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ad_subscription_packages",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            //migrationBuilder.AlterColumn<double>(
            //    name: "MaxAdsPerPeriod",
            //    table: "ad_subscription_packages",
            //    type: "double precision",
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "text");

            migrationBuilder.Sql(
                @"ALTER TABLE ad_subscription_packages 
                  ALTER COLUMN ""MaxAdsPerPeriod"" 
                  TYPE double precision 
                  USING ""MaxAdsPerPeriod""::double precision;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Price",
                table: "ad_subscription_packages",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "MaxAdsPerPeriod",
                table: "ad_subscription_packages",
                type: "text",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }
    }
}
