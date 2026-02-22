using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GESTION_PANIER.Migrations.GESTION_PANIER
{
    /// <inheritdoc />
    public partial class AddSearchCountToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchCount",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchCount",
                table: "Product");
        }
    }
}
