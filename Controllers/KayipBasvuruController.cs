using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class KayipBasvuruController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KayipBasvuruController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? arama,
            int? kategoriId,
            string? durum)
        {
            var sorgu = _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Vatandas)
                .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var aranan = arama.Trim();
                sorgu = sorgu.Where(x =>
                    x.EsyaAdi.Contains(aranan) ||
                    x.BasvuruNo.Contains(aranan) ||
                    (x.Vatandas != null &&
                     (x.Vatandas.Ad.Contains(aranan) ||
                      x.Vatandas.Soyad.Contains(aranan) ||
                      (x.Vatandas.Email != null &&
                       x.Vatandas.Email.Contains(aranan)))) ||
                    (x.Marka != null && x.Marka.Contains(aranan)) ||
                    (x.KayipYeri != null && x.KayipYeri.Contains(aranan)));
            }

            if (kategoriId.HasValue)
            {
                sorgu = sorgu.Where(x => x.KategoriId == kategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                sorgu = sorgu.Where(x => x.Durum == durum);
            }

            var sonuc = await sorgu
                .OrderByDescending(x => x.BasvuruTarihi)
                .ToListAsync();

            ViewBag.Arama = arama;
            ViewBag.KategoriId = kategoriId;
            ViewBag.Durum = durum;
            ViewBag.Kategoriler = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Kategoriler
                    .AsNoTracking()
                    .Where(x => x.AktifMi)
                    .OrderBy(x => x.Ad)
                    .ToListAsync(),
                "Id",
                "Ad",
                kategoriId);

            var durumlar = new[]
            {
                "Yeni Başvuru",
                "İnceleniyor",
                "Eşleşme Aranıyor",
                "Eşleşme Bulundu",
                "Vatandaşa Haber Verildi",
                "Teslim Edildi",
                "Çözüldü",
                "Pasif"
            };

            ViewBag.Durumlar = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                durumlar,
                durum);

            return View(sonuc);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basvuru = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Vatandas)
                .Include(x => x.Resimler.Where(r => r.AktifMi).OrderBy(r => r.SiraNumarasi))
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (basvuru == null)
            {
                return NotFound();
            }

            var eslesenEsyalar = new List<KayipEsya>();
            if (!string.IsNullOrWhiteSpace(basvuru.EsyaAdi))
            {
                var esyaAdi = basvuru.EsyaAdi.Trim().ToLowerInvariant();
                eslesenEsyalar = await _context.KayipEsyalar
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                    .Where(x => x.AktifMi)
                    .Where(x =>
                        x.KategoriId == basvuru.KategoriId ||
                        x.EsyaAdi.ToLower().Contains(esyaAdi) ||
                        (basvuru.Marka != null && x.Marka != null &&
                         x.Marka.ToLower().Contains(basvuru.Marka.ToLower())))
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Take(10)
                    .ToListAsync();
            }

            ViewBag.EslesenEsyalar = eslesenEsyalar;

            var durumlar = new[]
            {
                "Yeni Başvuru",
                "İnceleniyor",
                "Eşleşme Aranıyor",
                "Eşleşme Bulundu",
                "Vatandaşa Haber Verildi",
                "Teslim Edildi",
                "Çözüldü",
                "Pasif"
            };

            ViewBag.Durumlar = durumlar;

            return View(basvuru);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(
            int id,
            string durum,
            string? adminNotu)
        {
            var basvuru = await _context.KayipBildirimleri.FindAsync(id);
            if (basvuru == null)
            {
                return NotFound();
            }

            basvuru.Durum = durum;

            if (!string.IsNullOrWhiteSpace(adminNotu))
            {
                var onceki = string.IsNullOrWhiteSpace(basvuru.AdminNotu) ? "" : basvuru.AdminNotu + Environment.NewLine;
                basvuru.AdminNotu = $"{onceki}[{DateTime.Now:dd.MM.yyyy HH:mm}] {adminNotu.Trim()}";
            }

            basvuru.GuncellenmeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Başvuru durumu başarıyla \"{durum}\" olarak güncellendi.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}