using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesafioByCoders.Api.Migrations
{
    /// <inheritdoc />
    public partial class Addindexesforstorenameandtransactionstore_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_transactions_store_id",
                table: "transactions",
                newName: "ix_transactions_store_id");

            migrationBuilder.RenameIndex(
                name: "IX_stores_name",
                table: "stores",
                newName: "ix_stores_name_search");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_transactions_store_id",
                table: "transactions",
                newName: "IX_transactions_store_id");

            migrationBuilder.RenameIndex(
                name: "ix_stores_name_search",
                table: "stores",
                newName: "IX_stores_name");
        }
    }
}
