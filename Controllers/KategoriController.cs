using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KategoriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KategoriController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var kategoriler = await _context.Kategoriler
                .AsNoTracking()
                .OrderByDescending(x => x.AktifMi)
                .ThenBy(x => x.Ad)
                .ToListAsync();

            return View(kategoriler);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Kategori
            {
                AktifMi = true,
                OlusturmaTarihi = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Kategori model)
        {
            ModelState.Remove(nameof(Kategori.OlusturmaTarihi));

            model.Ad = model.Ad?.Trim() ?? string.Empty;
            model.Aciklama = MetniTemizle(model.Aciklama);

            if (string.IsNullOrWhiteSpace(model.Ad))
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Kategori adı zorunludur.");
            }

            var ayniKategoriVarMi =
                await _context.Kategoriler.AnyAsync(x =>
                    x.Ad.ToLower() == model.Ad.ToLower());

            if (ayniKategoriVarMi)
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Bu kategori zaten kayıtlıdır.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AktifMi = true;
            model.OlusturmaTarihi = DateTime.Now;

            _context.Kategoriler.Add(model);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kategori başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kategori = await _context.Kategoriler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (kategori == null)
            {
                return NotFound();
            }

            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Kategori model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            model.Ad = model.Ad?.Trim() ?? string.Empty;
            model.Aciklama = MetniTemizle(model.Aciklama);

            if (string.IsNullOrWhiteSpace(model.Ad))
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Kategori adı zorunludur.");
            }

            var ayniKategoriVarMi =
                await _context.Kategoriler.AnyAsync(x =>
                    x.Id != model.Id &&
                    x.Ad.ToLower() == model.Ad.ToLower());

            if (ayniKategoriVarMi)
            {
                ModelState.AddModelError(
                    nameof(model.Ad),
                    "Bu kategori adı başka bir kayıtta kullanılmaktadır.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var mevcutKategori =
                await _context.Kategoriler.FindAsync(id);

            if (mevcutKategori == null)
            {
                return NotFound();
            }

            mevcutKategori.Ad = model.Ad;
            mevcutKategori.Aciklama = model.Aciklama;
            mevcutKategori.AktifMi = model.AktifMi;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kategori başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id)
        {
            var kategori =
                await _context.Kategoriler.FindAsync(id);

            if (kategori == null)
            {
                return NotFound();
            }

            kategori.AktifMi = !kategori.AktifMi;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                kategori.AktifMi
                    ? "Kategori aktif hâle getirildi."
                    : "Kategori pasif hâle getirildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var kullaniliyorMu =
                await _context.KayipEsyalar
                    .AnyAsync(x => x.KategoriId == id) ||
                await _context.KayipBildirimleri
                    .AnyAsync(x => x.KategoriId == id);

            if (kullaniliyorMu)
            {
                TempData["HataMesaji"] =
                    "Bu kategori bir kayıt tarafından kullanıldığı için silinemez. Bunun yerine pasifleştirebilirsiniz.";

                return RedirectToAction(nameof(Index));
            }

            var kategori = await _context.Kategoriler.FindAsync(id);

            if (kategori == null)
            {
                return NotFound();
            }

            _context.Kategoriler.Remove(kategori);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] = "Kategori başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TumunuYenile()
        {
            var esyaKategoriIdleri =
                await _context.KayipEsyalar
                    .Select(x => x.KategoriId)
                    .Distinct()
                    .ToListAsync();

            var bildirimKategoriIdleri =
                await _context.KayipBildirimleri
                    .Select(x => x.KategoriId)
                    .Distinct()
                    .ToListAsync();

            var kullanilanKategoriIdleri =
                esyaKategoriIdleri
                    .Concat(bildirimKategoriIdleri)
                    .Distinct()
                    .ToList();

            var silinecekler = await _context.Kategoriler
                .Where(x => !kullanilanKategoriIdleri.Contains(x.Id))
                .ToListAsync();

            if (silinecekler.Any())
            {
                _context.Kategoriler.RemoveRange(silinecekler);
                await _context.SaveChangesAsync();
            }

            var standartKategoriler = DbInitializer.StandartKategoriler();

            foreach (var ktg in standartKategoriler)
            {
                var mevcut =
                    await _context.Kategoriler.FirstOrDefaultAsync(
                        x => x.Ad == ktg.Ad);

                if (mevcut != null)
                {
                    mevcut.Aciklama = ktg.Aciklama;
                    mevcut.AktifMi = true;
                }
                else
                {
                    _context.Kategoriler.Add(
                        new Kategori
                        {
                            Ad = ktg.Ad,
                            Aciklama = ktg.Aciklama,
                            AktifMi = true,
                            OlusturmaTarihi = DateTime.Now
                        });
                }
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kategoriler başarıyla standart listeye göre yenilendi.";

            return RedirectToAction(nameof(Index));
        }

        private static string? MetniTemizle(string? deger)
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }
    }
}