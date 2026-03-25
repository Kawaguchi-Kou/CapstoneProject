using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PartnerId",
                table: "pois",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_pois_PartnerId",
                table: "pois",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_pois_accounts_PartnerId",
                table: "pois",
                column: "PartnerId",
                principalTable: "accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pois_accounts_PartnerId",
                table: "pois");

            migrationBuilder.DropIndex(
                name: "IX_pois_PartnerId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "pois");
        }
    }
}
