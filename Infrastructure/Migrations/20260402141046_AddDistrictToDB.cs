using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictToDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "City",
                table: "pois");

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "pois",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_districts_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pois_DistrictId",
                table: "pois",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_districts_LocationId",
                table: "districts",
                column: "LocationId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_districts_DistrictId",
                table: "pois");

            migrationBuilder.DropForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropIndex(
                name: "IX_pois_DistrictId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "pois");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "pois",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_locations_LocationId",
                table: "pois",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
