using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketing.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentIntentIdToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_payment_intent_id",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stripe_payment_intent_id",
                table: "orders");
        }
    }
}
