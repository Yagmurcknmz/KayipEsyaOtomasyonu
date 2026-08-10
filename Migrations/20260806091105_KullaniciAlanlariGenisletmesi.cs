using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciAlanlariGenisletmesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adres",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IlceMahalle",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "AspNetUsers",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adres",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IlceMahalle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "AspNetUsers");
        }
    }
}
