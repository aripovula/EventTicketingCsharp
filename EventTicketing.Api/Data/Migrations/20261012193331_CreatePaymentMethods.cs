using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventTicketing.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatePaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stripe_payment_method_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    brand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    exp_month = table.Column<int>(type: "integer", nullable: false),
                    exp_year = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_methods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_user_id_default",
                table: "payment_methods",
                column: "user_id",
                unique: true,
                filter: "is_default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_methods");
        }
    }
}
