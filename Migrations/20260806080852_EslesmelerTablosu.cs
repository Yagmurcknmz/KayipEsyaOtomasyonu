using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayipEsyaOtomasyonu.Migrations
{
    /// <inheritdoc />
    public partial class EslesmelerTablosu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eslesmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KayipBildirimiId = table.Column<int>(type: "int", nullable: false),
                    KayipEsyaId = table.Column<int>(type: "int", nullable: false),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Skor = table.Column<int>(type: "int", nullable: false),
                    EslesmeDetay = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminNotu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OnaylayanAdmin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eslesmeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eslesmeler_KayipBildirimleri_KayipBildirimiId",
                        column: x => x.KayipBildirimiId,
                        principalTable: "KayipBildirimleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eslesmeler_KayipEsyalar_KayipEsyaId",
                        column: x => x.KayipEsyaId,
                        principalTable: "KayipEsyalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eslesmeler_Basvuru_Esya",
                table: "Eslesmeler",
                columns: new[] { "KayipBildirimiId", "KayipEsyaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Eslesmeler_KayipEsyaId",
                table: "Eslesmeler",
                column: "KayipEsyaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eslesmeler");
        }
    }
}
