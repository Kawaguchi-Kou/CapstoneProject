using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteCascadeForDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_districts_DistrictId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_districts_DistrictId",
                table: "pois",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_districts_DistrictId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_districts_DistrictId",
                table: "pois",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
