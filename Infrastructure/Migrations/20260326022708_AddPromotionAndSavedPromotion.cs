using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionAndSavedPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_advertisements_pois_POIId",
                table: "advertisements");

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Terms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SaveCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.PromotionId);
                    table.ForeignKey(
                        name: "FK_promotions_advertisements_AdId",
                        column: x => x.AdId,
                        principalTable: "advertisements",
                        principalColumn: "AdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_promotions",
                columns: table => new
                {
                    SavedPromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_promotions", x => x.SavedPromotionId);
                    table.ForeignKey(
                        name: "FK_saved_promotions_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_promotions_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_AdId",
                table: "promotions",
                column: "AdId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_promotions_AccountId",
                table: "saved_promotions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_promotions_PromotionId_AccountId",
                table: "saved_promotions",
                columns: new[] { "PromotionId", "AccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_advertisements_pois_POIId",
                table: "advertisements",
                column: "POIId",
                principalTable: "pois",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_advertisements_pois_POIId",
                table: "advertisements");

            migrationBuilder.DropTable(
                name: "saved_promotions");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.AddForeignKey(
                name: "FK_advertisements_pois_POIId",
                table: "advertisements",
                column: "POIId",
                principalTable: "pois",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
