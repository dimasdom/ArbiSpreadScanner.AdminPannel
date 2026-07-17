using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbiScannerAdminPanel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPaymentSubscriptionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_EndDate",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptionPayments_UserId",
                table: "UserSubscriptionPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_UserId_EndDate",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptionPayments_UserId",
                table: "UserSubscriptionPayments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UserId",
                table: "Payments");
        }
    }
}
