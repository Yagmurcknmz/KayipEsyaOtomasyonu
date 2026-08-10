using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class BildirimVeTeslimTablolari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AliciUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KayipBildirimiId = table.Column<int>(type: "int", nullable: true),
                    EslesmeId = table.Column<int>(type: "int", nullable: true),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Turu = table.Column<int>(type: "int", nullable: false),
                    OkunduMu = table.Column<bool>(type: "bit", nullable: false),
                    OkunmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_AspNetUsers_AliciUserId",
                        column: x => x.AliciUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Eslesmeler_EslesmeId",
                        column: x => x.EslesmeId,
                        principalTable: "Eslesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bildirimler_KayipBildirimleri_KayipBildirimiId",
                        column: x => x.KayipBildirimiId,
                        principalTable: "KayipBildirimleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeslimIslemleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EslesmeId = table.Column<int>(type: "int", nullable: false),
                    TeslimEdenUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TeslimAlanKisi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TcKimlikNo = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    IletisimTelefonu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TeslimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeslimSaati = table.Column<TimeSpan>(type: "time", nullable: true),
                    TeslimYeri = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TeslimSekli = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImzaOnayi = table.Column<bool>(type: "bit", nullable: false),
                    EkNotlar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeslimIslemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeslimIslemleri_AspNetUsers_TeslimEdenUserId",
                        column: x => x.TeslimEdenUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeslimIslemleri_Eslesmeler_EslesmeId",
                        column: x => x.EslesmeId,
                        principalTable: "Eslesmeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_AliciUserId",
                table: "Bildirimler",
                column: "AliciUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_EslesmeId",
                table: "Bildirimler",
                column: "EslesmeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KayipBildirimiId",
                table: "Bildirimler",
                column: "KayipBildirimiId");

            migrationBuilder.CreateIndex(
                name: "IX_TeslimIslemleri_EslesmeId",
                table: "TeslimIslemleri",
                column: "EslesmeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeslimIslemleri_TeslimEdenUserId",
                table: "TeslimIslemleri",
                column: "TeslimEdenUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "TeslimIslemleri");
        }
    }
}
