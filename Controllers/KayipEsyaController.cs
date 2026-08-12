using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class KayipEsyaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KayipEsyaController> _logger;
        private readonly IResimYuklemeServisi _resimServisi;

        public KayipEsyaController(
            ApplicationDbContext context,
            ILogger<KayipEsyaController> logger,
            IResimYuklemeServisi resimServisi)
        {
            _context = context;
            _logger = logger;
            _resimServisi = resimServisi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? arama,
            int? kategoriId,
            string? durum)
        {
            var sorgu = _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Resimler.Where(r => r.AktifMi))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                var aranan = arama.Trim();

                sorgu = sorgu.Where(x =>
                    x.EsyaAdi.Contains(aranan) ||
                    (x.Marka != null && x.Marka.Contains(aranan)) ||
                    (x.Model != null && x.Model.Contains(aranan)) ||
                    (x.Renk != null && x.Renk.Contains(aranan)) ||
                    (x.BulunmaYeri != null && x.BulunmaYeri.Contains(aranan)));
            }

            if (kategoriId.HasValue)
            {
                sorgu = sorgu.Where(x =>
                    x.KategoriId == kategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                sorgu = sorgu.Where(x =>
                    x.Durum == durum);
            }

            var kayipEsyalar = await sorgu
                .OrderByDescending(x => x.BulunmaTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            await FiltreListeleriniHazirla(
                kategoriId,
                durum);

            ViewBag.Arama = arama;

            return View(kayipEsyalar);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await KategoriListesiniHazirla();

            var kayipEsya = new KayipEsya
            {
                BulunmaTarihi = DateTime.Today,
                Durum = "Yeni Kayıt",
                AktifMi = true
            };

            return View(kayipEsya);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KayipEsya kayipEsya)
        {
            ModelState.Remove(nameof(KayipEsya.Kategori));
            ModelState.Remove(nameof(KayipEsya.Resimler));

            if (kayipEsya.BulunmaTarihi.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(kayipEsya.BulunmaTarihi),
                    "Bulunma tarihi bugünden ileri olamaz.");
            }

            var kategoriVarMi =
                await _context.Kategoriler.AnyAsync(x =>
                    x.Id == kayipEsya.KategoriId &&
                    x.AktifMi);

            if (!kategoriVarMi)
            {
                ModelState.AddModelError(
                    nameof(kayipEsya.KategoriId),
                    "Geçerli bir kategori seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                await KategoriListesiniHazirla(
                    kayipEsya.KategoriId);

                return View(kayipEsya);
            }

            kayipEsya.EsyaAdi =
                kayipEsya.EsyaAdi.Trim();

            kayipEsya.Marka =
                MetniTemizle(kayipEsya.Marka);

            kayipEsya.Model =
                MetniTemizle(kayipEsya.Model);

            kayipEsya.Renk =
                MetniTemizle(kayipEsya.Renk);

            kayipEsya.SeriNo =
                MetniTemizle(kayipEsya.SeriNo);

            kayipEsya.RafNo =
                MetniTemizle(kayipEsya.RafNo);

            kayipEsya.BulunmaYeri =
                string.IsNullOrWhiteSpace(kayipEsya.BulunmaYeri)
                    ? string.Empty
                    : kayipEsya.BulunmaYeri.Trim();

            kayipEsya.Mahalle =
                MetniTemizle(kayipEsya.Mahalle);

            kayipEsya.Birim =
                MetniTemizle(kayipEsya.Birim);

            kayipEsya.AyirtEdiciOzellik =
                MetniTemizle(kayipEsya.AyirtEdiciOzellik);

            kayipEsya.Aciklama =
                MetniTemizle(kayipEsya.Aciklama);

            kayipEsya.AdresDetayi = MetniTemizle(kayipEsya.AdresDetayi);

            kayipEsya.AktifMi = true;

            if (string.IsNullOrWhiteSpace(kayipEsya.Durum))
            {
                kayipEsya.Durum = "Yeni Kayıt";
            }

            try
            {
                await _context.KayipEsyalar.AddAsync(kayipEsya);
                await _context.SaveChangesAsync();

                var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var dosyalar = Request.Form.Files
                    .Where(f =>
                        f.Name.Equals("ResimDosyalari", StringComparison.OrdinalIgnoreCase) ||
                        f.Name.Equals("ResimDosyalari[]", StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .ToList();

                if (dosyalar.Count > 0)
                {
                    var yuklemeler = await _resimServisi.CokluYukleAsync(
                        dosyalar,
                        "kayip-esya",
                        300,
                        20 * 1024 * 1024,
                        kullaniciId);

                    for (int i = 0; i < yuklemeler.Count; i++)
                    {
                        var y = yuklemeler[i];
                        if (y.Basarili && !string.IsNullOrWhiteSpace(y.DosyaYolu))
                        {
                            _context.KayipEsyaResimler.Add(new KayipEsyaResim
                            {
                                KayipEsyaId = kayipEsya.Id,
                                DosyaYolu = y.DosyaYolu!,
                                ThumbnailYolu = y.ThumbnailYolu,
                                SiraNumarasi = i,
                                VarsayilanResimMi = i == 0,
                                YukleyenKullaniciId = kullaniciId,
                                YuklenmeTarihi = DateTime.Now,
                                AktifMi = true
                            });
                        }
                    }

                    if (yuklemeler.Any(y => y.Basarili))
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["BasariliMesaj"] =
                    "Kayıp eşya kaydı başarıyla oluşturuldu." +
                    (dosyalar.Count > 0 ? $" {dosyalar.Count} adet fotoğraf yüklendi." : "");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Kayıp eşya kaydedilirken hata oluştu.");

                ModelState.AddModelError(
                    string.Empty,
                    "Kayıp eşya kaydedilirken bir hata oluştu.");

                await KategoriListesiniHazirla(
                    kayipEsya.KategoriId);

                return View(kayipEsya);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kayipEsyaDetay = await _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Resimler.Where(r => r.AktifMi).OrderBy(r => r.SiraNumarasi))
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (kayipEsyaDetay == null)
            {
                return NotFound();
            }

            return View(kayipEsyaDetay);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kayipEsyaDuzenle =
                await _context.KayipEsyalar
                    .Include(x => x.Resimler.Where(r => r.AktifMi).OrderBy(r => r.SiraNumarasi))
                    .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (kayipEsyaDuzenle == null)
            {
                return NotFound();
            }

            await KategoriListesiniHazirla(
                kayipEsyaDuzenle.KategoriId);

            return View(kayipEsyaDuzenle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            KayipEsya kayipEsya,
            List<IFormFile>? ResimDosyalari)
        {
            if (id != kayipEsya.Id)
            {
                return BadRequest();
            }

            ModelState.Remove(nameof(KayipEsya.Kategori));
            ModelState.Remove(nameof(KayipEsya.Resimler));
            ModelState.Remove(nameof(KayipEsya.Enlem));
            ModelState.Remove(nameof(KayipEsya.Boylam));
            ModelState.Remove(nameof(KayipEsya.AdresDetayi));

            if (kayipEsya.BulunmaTarihi.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(kayipEsya.BulunmaTarihi),
                    "Bulunma tarihi bugünden ileri olamaz.");
            }

            var kategoriVarMi =
                await _context.Kategoriler.AnyAsync(x =>
                    x.Id == kayipEsya.KategoriId &&
                    x.AktifMi);

            if (!kategoriVarMi)
            {
                ModelState.AddModelError(
                    nameof(kayipEsya.KategoriId),
                    "Geçerli bir kategori seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                await KategoriListesiniHazirla(
                    kayipEsya.KategoriId);

                return View(kayipEsya);
            }

            var mevcutKayipEsya =
                await _context.KayipEsyalar
                    .Include(x => x.Resimler.Where(r => r.AktifMi))
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (mevcutKayipEsya == null)
            {
                return NotFound();
            }

            mevcutKayipEsya.EsyaAdi =
                kayipEsya.EsyaAdi.Trim();

            mevcutKayipEsya.KategoriId =
                kayipEsya.KategoriId;

            mevcutKayipEsya.Marka =
                MetniTemizle(kayipEsya.Marka);

            mevcutKayipEsya.Model =
                MetniTemizle(kayipEsya.Model);

            mevcutKayipEsya.Renk =
                MetniTemizle(kayipEsya.Renk);

            mevcutKayipEsya.SeriNo =
                MetniTemizle(kayipEsya.SeriNo);

            mevcutKayipEsya.RafNo =
                MetniTemizle(kayipEsya.RafNo);

            mevcutKayipEsya.BulunmaTarihi =
                kayipEsya.BulunmaTarihi.Date;

            mevcutKayipEsya.BulunmaYeri =
                string.IsNullOrWhiteSpace(kayipEsya.BulunmaYeri)
                    ? string.Empty
                    : kayipEsya.BulunmaYeri.Trim();

            mevcutKayipEsya.Mahalle =
                MetniTemizle(kayipEsya.Mahalle);

            mevcutKayipEsya.Birim =
                MetniTemizle(kayipEsya.Birim);

            mevcutKayipEsya.AyirtEdiciOzellik =
                MetniTemizle(kayipEsya.AyirtEdiciOzellik);

            mevcutKayipEsya.Aciklama =
                MetniTemizle(kayipEsya.Aciklama);

            mevcutKayipEsya.Enlem = kayipEsya.Enlem;
            mevcutKayipEsya.Boylam = kayipEsya.Boylam;
            mevcutKayipEsya.AdresDetayi = MetniTemizle(kayipEsya.AdresDetayi);

            mevcutKayipEsya.Durum =
                kayipEsya.Durum;

            mevcutKayipEsya.AktifMi =
                kayipEsya.AktifMi;

            mevcutKayipEsya.GuncellenmeTarihi = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();

                var ekDosyalar = (ResimDosyalari ?? Enumerable.Empty<IFormFile>())
                    .Concat(Request.Form.Files
                        .Where(f =>
                            f.Name.Equals("ResimDosyalari", StringComparison.OrdinalIgnoreCase) ||
                            f.Name.Equals("ResimDosyalari[]", StringComparison.OrdinalIgnoreCase)))
                    .DistinctBy(f => f.FileName + f.Length)
                    .Where(f => f.Length > 0)
                    .ToList();

                if (ekDosyalar.Count > 0)
                {
                    var mevcutResimSayisi = mevcutKayipEsya.Resimler?.Count ?? 0;
                    var kapasite = Math.Max(0, 5 - mevcutResimSayisi);
                    if (kapasite > 0)
                    {
                        var yuklenecekler = ekDosyalar.Take(kapasite).ToList();
                        var yukleyenKullaniciId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

                        var yuklemeler = await _resimServisi.CokluYukleAsync(
                            yuklenecekler,
                            "kayip-esya",
                            300,
                            20 * 1024 * 1024,
                            yukleyenKullaniciId);

                        var yeniSira = mevcutResimSayisi;
                        var ilkKayitMi = mevcutResimSayisi == 0;
                        for (int i = 0; i < yuklemeler.Count; i++)
                        {
                            var y = yuklemeler[i];
                            if (y.Basarili && !string.IsNullOrWhiteSpace(y.DosyaYolu))
                            {
                                _context.KayipEsyaResimler.Add(new KayipEsyaResim
                                {
                                    KayipEsyaId = mevcutKayipEsya.Id,
                                    DosyaYolu = y.DosyaYolu!,
                                    ThumbnailYolu = y.ThumbnailYolu,
                                    SiraNumarasi = yeniSira++,
                                    VarsayilanResimMi = ilkKayitMi && i == 0,
                                    YukleyenKullaniciId = yukleyenKullaniciId,
                                    YuklenmeTarihi = DateTime.Now,
                                    AktifMi = true
                                });
                            }
                        }

                        if (yuklemeler.Any(y => y.Basarili))
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                TempData["BasariliMesaj"] =
                    "Kayıp eşya kaydı başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogError(
                    exception,
                    "Kayıp eşya güncellenirken eş zamanlılık hatası oluştu.");

                if (!await KayipEsyaVarMi(kayipEsya.Id))
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Kayıt güncellenirken bir hata oluştu.");
            }

            await KategoriListesiniHazirla(
                kayipEsya.KategoriId);

            return View(kayipEsya);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasifeAl(int id)
        {
            var kayipEsyaPasif =
                await _context.KayipEsyalar.FindAsync(id);

            if (kayipEsyaPasif == null)
            {
                return NotFound();
            }

            kayipEsyaPasif.AktifMi = false;
            kayipEsyaPasif.GuncellenmeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kayıp eşya kaydı pasif hâle getirildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AktifEt(int id)
        {
            var kayipEsyaAktif =
                await _context.KayipEsyalar.FindAsync(id);

            if (kayipEsyaAktif == null)
            {
                return NotFound();
            }

            kayipEsyaAktif.AktifMi = true;
            kayipEsyaAktif.GuncellenmeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                "Kayıp eşya kaydı aktif hâle getirildi.";

            return RedirectToAction(nameof(Index));
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

        private async Task FiltreListeleriniHazirla(
            int? kategoriId,
            string? durum)
        {
            await KategoriListesiniHazirla(kategoriId);

            var durumlar = new[]
            {
                "Yeni Kayıt",
                "Depoda",
                "Eşleşme Bulundu",
                "Vatandaşa Haber Verildi",
                "Teslim Bekliyor",
                "Teslim Edildi",
                "Arşivlendi"
            };

            ViewBag.Durumlar = new SelectList(
                durumlar,
                durum);
        }

        private async Task<bool> KayipEsyaVarMi(int id)
        {
            return await _context.KayipEsyalar
                .AnyAsync(x => x.Id == id);
        }

        private static string? MetniTemizle(string? deger)
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }
    }
}
