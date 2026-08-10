using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class AdminNotuAlaniEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotu",
                table: "KayipBildirimleri",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotu",
                table: "KayipBildirimleri");
        }
    }
}
