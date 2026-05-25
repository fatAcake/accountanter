using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_transactions_date",
                table: "transactions",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_deleted_date",
                table: "transactions",
                columns: new[] { "deleted", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_date_active",
                table: "transactions",
                column: "date",
                filter: "\"deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_lines_account_id_transaction_id",
                table: "transaction_lines",
                columns: new[] { "account_id", "transaction_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_lines_counterparty_id_account_id",
                table: "transaction_lines",
                columns: new[] { "counterparty_id", "account_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_date",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_deleted_date",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_date_active",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transaction_lines_account_id_transaction_id",
                table: "transaction_lines");

            migrationBuilder.DropIndex(
                name: "IX_transaction_lines_counterparty_id_account_id",
                table: "transaction_lines");
        }
    }
}
