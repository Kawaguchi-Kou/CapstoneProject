using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class BackfillPaymentCreatedAtExpiresAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ad_payments
SET
    ""CreatedAt"" = CASE
        WHEN ""CreatedAt"" = '-infinity'::timestamptz THEN COALESCE(""PaidAt"", NOW())
        ELSE ""CreatedAt""
    END,
    ""ExpiresAt"" = CASE
        WHEN ""ExpiresAt"" = '-infinity'::timestamptz THEN COALESCE(""PaidAt"", NOW()) + INTERVAL '15 minutes'
        ELSE ""ExpiresAt""
    END
WHERE ""CreatedAt"" = '-infinity'::timestamptz
   OR ""ExpiresAt"" = '-infinity'::timestamptz;

UPDATE ad_payments
SET ""PaymentStatus"" = 2
WHERE ""PaymentStatus"" = 0
  AND ""ExpiresAt"" <= NOW();
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: data backfill cannot be safely reversed.
        }
    }
}
