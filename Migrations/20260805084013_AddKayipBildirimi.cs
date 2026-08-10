using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class AddKayipBildirimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KayipBildirimleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BasvuruNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VatandasId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EsyaAdi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Renk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KayipTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KayipYeri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AyirtEdiciOzellik = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BasvuruTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KayipBildirimleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KayipBildirimleri_AspNetUsers_VatandasId",
                        column: x => x.VatandasId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KayipBildirimleri_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KayipBildirimleri_KategoriId",
                table: "KayipBildirimleri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_KayipBildirimleri_VatandasId",
                table: "KayipBildirimleri",
                column: "VatandasId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KayipBildirimleri");
        }
    }
}
