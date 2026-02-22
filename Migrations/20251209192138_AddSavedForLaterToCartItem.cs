using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GESTION_PANIER.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedForLaterToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SavedForLater",
                table: "CartItem",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavedForLater",
                table: "CartItem");
        }
    }
}
