using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partner_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BusinessAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BusinessPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BusinessEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BusinessLicenseUrl = table.Column<string>(type: "text", nullable: true),
                    BusinessAvatarUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partner_profiles_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partner_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BusinessAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BusinessPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BusinessEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BusinessLicenseUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partner_requests_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_partner_profiles_AccountId",
                table: "partner_profiles",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_requests_AccountId",
                table: "partner_requests",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_profiles");

            migrationBuilder.DropTable(
                name: "partner_requests");
        }
    }
}
