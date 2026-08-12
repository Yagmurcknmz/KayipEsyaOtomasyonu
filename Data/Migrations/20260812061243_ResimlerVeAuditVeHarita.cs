using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResimlerVeAuditVeHarita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresDetayi",
                table: "KayipEsyalar",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Boylam",
                table: "KayipEsyalar",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Enlem",
                table: "KayipEsyalar",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresDetayi",
                table: "KayipBildirimleri",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Boylam",
                table: "KayipBildirimleri",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Enlem",
                table: "KayipBildirimleri",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLoglar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    TabloAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KayitId = table.Column<long>(type: "bigint", nullable: true),
                    KayitAnahtari = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EskiDegerlerJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YeniDegerlerJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IpAdresi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLoglar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLoglar_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KayipBildirimiResimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayipBildirimiId = table.Column<int>(type: "int", nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SiraNumarasi = table.Column<int>(type: "int", nullable: false),
                    VarsayilanResimMi = table.Column<bool>(type: "bit", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    YukleyenKullaniciId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    YuklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KayipBildirimiResimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KayipBildirimiResimler_KayipBildirimleri_KayipBildirimiId",
                        column: x => x.KayipBildirimiId,
                        principalTable: "KayipBildirimleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KayipEsyaResimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayipEsyaId = table.Column<int>(type: "int", nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SiraNumarasi = table.Column<int>(type: "int", nullable: false),
                    VarsayilanResimMi = table.Column<bool>(type: "bit", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    YukleyenKullaniciId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    YuklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KayipEsyaResimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KayipEsyaResimler_KayipEsyalar_KayipEsyaId",
                        column: x => x.KayipEsyaId,
                        principalTable: "KayipEsyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLoglar_Tablo_Kayit_Tarih",
                table: "AuditLoglar",
                columns: new[] { "TabloAdi", "KayitId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLoglar_Tarih",
                table: "AuditLoglar",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLoglar_User_Tarih",
                table: "AuditLoglar",
                columns: new[] { "UserId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_KayipBildirimiResimler_Basvuru_Sira",
                table: "KayipBildirimiResimler",
                columns: new[] { "KayipBildirimiId", "SiraNumarasi" });

            migrationBuilder.CreateIndex(
                name: "IX_KayipEsyaResimler_Esya_Sira",
                table: "KayipEsyaResimler",
                columns: new[] { "KayipEsyaId", "SiraNumarasi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLoglar");

            migrationBuilder.DropTable(
                name: "KayipBildirimiResimler");

            migrationBuilder.DropTable(
                name: "KayipEsyaResimler");

            migrationBuilder.DropColumn(
                name: "AdresDetayi",
                table: "KayipEsyalar");

            migrationBuilder.DropColumn(
                name: "Boylam",
                table: "KayipEsyalar");

            migrationBuilder.DropColumn(
                name: "Enlem",
                table: "KayipEsyalar");

            migrationBuilder.DropColumn(
                name: "AdresDetayi",
                table: "KayipBildirimleri");

            migrationBuilder.DropColumn(
                name: "Boylam",
                table: "KayipBildirimleri");

            migrationBuilder.DropColumn(
                name: "Enlem",
                table: "KayipBildirimleri");
        }
    }
}
