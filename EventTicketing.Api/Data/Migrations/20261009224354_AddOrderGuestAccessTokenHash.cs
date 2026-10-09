using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketing.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderGuestAccessTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "guest_access_token_hash",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guest_access_token_hash",
                table: "orders");
        }
    }
}
