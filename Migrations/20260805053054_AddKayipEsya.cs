using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class AddKayipEsya : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KayipEsyalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EsyaAdi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Renk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeriNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AyirtEdiciOzellik = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BulunmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BulunmaYeri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mahalle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Birim = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RafNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KayipEsyalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KayipEsyalar_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KayipEsyalar_KategoriId",
                table: "KayipEsyalar",
                column: "KategoriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KayipEsyalar");
        }
    }
}
