using System.Globalization;
using System.Security.Claims;
using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Vatandas")]
    public class KayipBildirimiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KayipBildirimiController> _logger;

        public KayipBildirimiController(
            ApplicationDbContext context,
            ILogger<KayipBildirimiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await KategoriListesiniHazirla();

            var model = new KayipBildirimiOlusturViewModel
            {
                KayipTarihi = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            KayipBildirimiOlusturViewModel viewModel)
        {
            var vatandasId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(vatandasId))
            {
                return Challenge();
            }

            if (viewModel.KayipTarihi.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(viewModel.KayipTarihi),
                    "Kayıp tarihi bugünden ileri olamaz.");
            }

            if (viewModel.KategoriId.HasValue)
            {
                var kategoriVarMi =
                    await _context.Kategoriler.AnyAsync(x =>
                        x.Id == viewModel.KategoriId.Value &&
                        x.AktifMi);

                if (!kategoriVarMi)
                {
                    ModelState.AddModelError(
                        nameof(viewModel.KategoriId),
                        "Geçerli bir kategori seçiniz.");
                }
            }

            if (!ModelState.IsValid)
            {
                await KategoriListesiniHazirla(
                    viewModel.KategoriId);

                return View(viewModel);
            }

            var kayipBildirimi = new KayipBildirimi
            {
                BasvuruNo = await BasvuruNumarasiOlustur(),
                VatandasId = vatandasId,

                EsyaAdi = viewModel.EsyaAdi.Trim(),
                KategoriId = viewModel.KategoriId!.Value,

                Marka = BosIseNull(viewModel.Marka),
                Model = BosIseNull(viewModel.Model),
                Renk = BosIseNull(viewModel.Renk),

                KayipTarihi = viewModel.KayipTarihi.Date,
                KayipYeri = viewModel.KayipYeri.Trim(),

                AyirtEdiciOzellik =
                    BosIseNull(viewModel.AyirtEdiciOzellik),

                Aciklama =
                    BosIseNull(viewModel.Aciklama),

                Durum = "Başvuru Alındı",
                BasvuruTarihi = DateTime.Now,
                GuncellenmeTarihi = null,
                AktifMi = true
            };

            try
            {
                await _context.KayipBildirimleri.AddAsync(
                    kayipBildirimi);

                await _context.SaveChangesAsync();

                TempData["BasariliMesaj"] =
                    $"Başvurunuz başarıyla kaydedildi. " +
                    $"Başvuru numaranız: {kayipBildirimi.BasvuruNo}";

                return RedirectToAction(nameof(Basvurularim));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Kayıp bildirimi kaydedilirken hata oluştu.");

                ModelState.AddModelError(
                    string.Empty,
                    "Başvuru kaydedilirken bir hata oluştu.");

                await KategoriListesiniHazirla(
                    viewModel.KategoriId);

                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Basvurularim()
        {
            var vatandasId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(vatandasId))
            {
                return Challenge();
            }

            var basvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x =>
                    x.VatandasId == vatandasId &&
                    x.AktifMi)
                .OrderByDescending(x => x.BasvuruTarihi)
                .ToListAsync();

            return View(basvurular);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vatandasId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(vatandasId))
            {
                return Challenge();
            }

            var basvuru = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .FirstOrDefaultAsync(x =>
                    x.Id == id.Value &&
                    x.VatandasId == vatandasId &&
                    x.AktifMi);

            if (basvuru == null)
            {
                return NotFound();
            }

            return View(basvuru);
        }

        private async Task KategoriListesiniHazirla(
            int? seciliKategoriId = null)
        {
            var kategoriler = await _context.Kategoriler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Kategoriler = new SelectList(
                kategoriler,
                "Id",
                "Ad",
                seciliKategoriId);
        }

        private async Task<string> BasvuruNumarasiOlustur()
        {
            var yil = DateTime.Now.Year;
            var onEk = $"KB-{yil}-";

            var numaralar = await _context.KayipBildirimleri
                .AsNoTracking()
                .Where(x => x.BasvuruNo.StartsWith(onEk))
                .Select(x => x.BasvuruNo)
                .ToListAsync();

            var enBuyukSira = numaralar
                .Select(x => x.Split('-').LastOrDefault())
                .Select(x =>
                    int.TryParse(x, out var sira)
                        ? sira
                        : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{onEk}{enBuyukSira + 1:D6}";
        }

        private static string? BosIseNull(string? deger)
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }
    }
}
