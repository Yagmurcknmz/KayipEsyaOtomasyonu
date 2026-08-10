using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class EslesmeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EslesmeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? ara,
            int? kategoriId,
            EslesmeDurumu? durum)
        {
            var sorgu = _context.Eslesmeler
                .AsNoTracking()
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Kategori)
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Vatandas)
                .Include(x => x.KayipEsya)
                    .ThenInclude(x => x!.Kategori)
                .Where(x => x.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(ara))
            {
                var arama = ara.Trim().ToLowerInvariant();
                sorgu = sorgu.Where(x =>
                    x.KayipBildirimi!.EsyaAdi.ToLower().Contains(arama) ||
                    x.KayipEsya!.EsyaAdi.ToLower().Contains(arama) ||
                    x.KayipBildirimi!.Marka != null && x.KayipBildirimi.Marka.ToLower().Contains(arama) ||
                    x.KayipEsya!.Marka != null && x.KayipEsya.Marka.ToLower().Contains(arama) ||
                    x.KayipBildirimi!.Vatandas != null &&
                    (x.KayipBildirimi.Vatandas.Ad + " " + x.KayipBildirimi.Vatandas.Soyad).ToLower().Contains(arama));
            }

            if (kategoriId.HasValue && kategoriId > 0)
            {
                sorgu = sorgu.Where(x =>
                    x.KayipEsya!.KategoriId == kategoriId.Value ||
                    x.KayipBildirimi!.KategoriId == kategoriId.Value);
            }

            if (durum.HasValue)
            {
                sorgu = sorgu.Where(x => x.Durum == durum.Value);
            }

            var list = await sorgu
                .OrderByDescending(x => x.Durum == EslesmeDurumu.Beklemede ? 1 : 0)
                .ThenByDescending(x => x.OlusturmaTarihi)
                .ToListAsync();

            var kategoriler = await _context.Kategoriler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Kategoriler = kategoriler;
            ViewBag.Ara = ara;
            ViewBag.KategoriId = kategoriId;
            ViewBag.Durum = durum;

            var durumlar = Enum.GetValues(typeof(EslesmeDurumu))
                .Cast<EslesmeDurumu>()
                .Select(x => new
                {
                    Deger = x,
                    Ad = x.ToString()
                })
                .ToList();

            ViewBag.TumDurumlar = durumlar;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var eslesme = await _context.Eslesmeler
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Kategori)
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Vatandas)
                .Include(x => x.KayipEsya)
                    .ThenInclude(x => x!.Kategori)
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (eslesme == null) return NotFound();

            return View(eslesme);
        }

        [HttpPost, ActionName("OtomatikEsles")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtomatikEsles()
        {
            var aktifBasvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Vatandas)
                .Include(x => x.Kategori)
                .Where(x =>
                    x.AktifMi &&
                    (x.Durum == "Yeni Başvuru" ||
                     x.Durum == "İnceleniyor" ||
                     x.Durum == "Eşleşme Aranıyor"))
                .ToListAsync();

            var mevcutEslesmeler = await _context.Eslesmeler
                .AsNoTracking()
                .Select(x => new { x.KayipBildirimiId, x.KayipEsyaId })
                .ToListAsync();

            var mevcutSet = new HashSet<(int, int)>(
                mevcutEslesmeler.Select(x => (x.KayipBildirimiId, x.KayipEsyaId)));

            var aktifKayitlar = await _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x => x.AktifMi && x.Durum != "Teslim Edildi")
                .ToListAsync();

            int eklenen = 0;

            foreach (var basvuru in aktifBasvurular)
            {
                if (mevcutEslesmeler.Any(x =>
                    x.KayipBildirimiId == basvuru.Id &&
                    _context.Eslesmeler.AsNoTracking()
                        .Any(e =>
                            e.KayipBildirimiId == basvuru.Id &&
                            e.AktifMi &&
                            e.Durum == EslesmeDurumu.Onaylandi)))
                {
                    continue;
                }

                var basvuruAdLower = basvuru.EsyaAdi?.Trim().ToLowerInvariant() ?? "";
                var basvuruMarkaLower = basvuru.Marka?.Trim().ToLowerInvariant() ?? "";
                var basvuruRenkLower = basvuru.Renk?.Trim().ToLowerInvariant() ?? "";
                var basvuruOzellikLower = basvuru.AyirtEdiciOzellik?.Trim().ToLowerInvariant() ?? "";

                foreach (var kayit in aktifKayitlar)
                {
                    if (mevcutSet.Contains((basvuru.Id, kayit.Id)))
                    {
                        continue;
                    }

                    int skor = 0;
                    var detay = new StringBuilder();

                    if (basvuru.KategoriId > 0 && basvuru.KategoriId == kayit.KategoriId)
                    {
                        skor += 35;
                        detay.Append("Kategori eşleşmesi, ");
                    }

                    if (!string.IsNullOrEmpty(basvuruAdLower))
                    {
                        if (!string.IsNullOrEmpty(kayit.EsyaAdi) &&
                            kayit.EsyaAdi.ToLowerInvariant().Contains(basvuruAdLower))
                        {
                            skor += 30;
                            detay.Append("Eşya adı eşleşmesi, ");
                        }
                        else if (basvuruAdLower.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Any(kelime => !string.IsNullOrEmpty(kayit.EsyaAdi) &&
                                           kayit.EsyaAdi.ToLowerInvariant().Contains(kelime)))
                        {
                            skor += 15;
                            detay.Append("Eşya adı kelime eşleşmesi, ");
                        }
                    }

                    if (!string.IsNullOrEmpty(basvuruMarkaLower))
                    {
                        if (!string.IsNullOrEmpty(kayit.Marka) &&
                            kayit.Marka.ToLowerInvariant().Contains(basvuruMarkaLower))
                        {
                            skor += 15;
                            detay.Append("Marka eşleşmesi, ");
                        }
                    }

                    if (!string.IsNullOrEmpty(basvuruRenkLower))
                    {
                        if (!string.IsNullOrEmpty(kayit.Renk) &&
                            kayit.Renk.ToLowerInvariant().Contains(basvuruRenkLower))
                        {
                            skor += 10;
                            detay.Append("Renk eşleşmesi, ");
                        }
                    }

                    if (!string.IsNullOrEmpty(basvuruOzellikLower))
                    {
                        if (!string.IsNullOrEmpty(kayit.AyirtEdiciOzellik) &&
                            kayit.AyirtEdiciOzellik.ToLowerInvariant()
                                .Intersect(basvuruOzellikLower).Count() > 5)
                        {
                            skor += 10;
                            detay.Append("Ayırt edici özellik benzerliği, ");
                        }
                    }

                    if (skor >= 25)
                    {
                        var yeni = new Eslesme
                        {
                            KayipBildirimiId = basvuru.Id,
                            KayipEsyaId = kayit.Id,
                            Tur = EslesmeTuru.Otomatik,
                            Durum = EslesmeDurumu.Beklemede,
                            Skor = skor > 100 ? 100 : skor,
                            EslesmeDetay = detay.ToString().Trim().TrimEnd(',')
                        };

                        _context.Eslesmeler.Add(yeni);
                        mevcutSet.Add((basvuru.Id, kayit.Id));
                        eklenen++;

                        if (skor >= 70)
                        {
                            basvuru.Durum = "Eşleşme Bulundu";
                            basvuru.GuncellenmeTarihi = DateTime.Now;
                            _context.KayipBildirimleri.Update(basvuru);
                        }
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
            }

            TempData["BasariliMesaj"] =
                $"Otomatik eşleştirme tamamlandı. {eklenen} yeni eşleşme önerisi oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(
            int id,
            string? adminNotu)
        {
            var eslesme = await _context.Eslesmeler
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(b => b!.Vatandas)
                .Include(x => x.KayipEsya)
                    .ThenInclude(e => e!.Kategori)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (eslesme == null) return NotFound();

            eslesme.Durum = EslesmeDurumu.Onaylandi;
            eslesme.IslemTarihi = DateTime.Now;
            eslesme.OnaylayanAdmin = User.Identity?.Name;
            eslesme.GuncellenmeTarihi = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(adminNotu))
            {
                eslesme.AdminNotu =
                    $"[{DateTime.Now:dd.MM.yyyy HH:mm}] (Onay) {adminNotu.Trim()}";
            }

            if (eslesme.KayipEsya != null)
            {
                eslesme.KayipEsya.Durum = "Eşleşme Bulundu";
                eslesme.KayipEsya.GuncellenmeTarihi = DateTime.Now;
            }

            if (eslesme.KayipBildirimi != null)
            {
                eslesme.KayipBildirimi.Durum = "Eşleşme Bulundu";
                eslesme.KayipBildirimi.GuncellenmeTarihi = DateTime.Now;

                if (!string.IsNullOrEmpty(eslesme.KayipBildirimi.VatandasId))
                {
                    _context.Bildirimler.Add(new Bildirim
                    {
                        AliciUserId = eslesme.KayipBildirimi.VatandasId,
                        KayipBildirimiId = eslesme.KayipBildirimi.Id,
                        EslesmeId = eslesme.Id,
                        Baslik = "🎉 Eşleşme Bulundu!",
                        Icerik = $"Değerli Vatandaşımız, #{eslesme.KayipBildirimi.Id} numaralı \"{eslesme.KayipBildirimi.EsyaAdi}\" başvurunuz için eşleşen bir " +
                                 $"\"{eslesme.KayipEsya?.EsyaAdi}\" ({eslesme.KayipEsya?.Kategori?.Ad}) eşyası bulundu. " +
                                 $"En kısa sürede Arnavutköy Belediyesi'nden teslim almak için lütfen haber bekleyiniz.",
                        Turu = BildirimTuru.EslesmeBulundu,
                        OkunduMu = false,
                        AktifMi = true,
                        OlusturulmaTarihi = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Eşleşme #{id} başarıyla ONAYLANDI. Eşya ve Başvuru durumları güncellendi. " +
                "Vatandaşa bildirim gönderildi!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reddet(
            int id,
            string? adminNotu)
        {
            var eslesme = await _context.Eslesmeler.FindAsync(id);
            if (eslesme == null) return NotFound();

            eslesme.Durum = EslesmeDurumu.Reddedildi;
            eslesme.IslemTarihi = DateTime.Now;
            eslesme.OnaylayanAdmin = User.Identity?.Name;
            eslesme.GuncellenmeTarihi = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(adminNotu))
            {
                eslesme.AdminNotu =
                    $"[{DateTime.Now:dd.MM.yyyy HH:mm}] (Ret) {adminNotu.Trim()}";
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] = $"Eşleşme #{id} reddedildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeslimEt(int id, string? adminNotu)
        {
            var eslesme = await _context.Eslesmeler
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(b => b!.Vatandas)
                .Include(x => x.KayipEsya)
                    .ThenInclude(e => e!.Kategori)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (eslesme == null) return NotFound();

            eslesme.Durum = EslesmeDurumu.TeslimEdildi;
            eslesme.IslemTarihi = DateTime.Now;
            eslesme.OnaylayanAdmin = User.Identity?.Name;
            eslesme.GuncellenmeTarihi = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(adminNotu))
            {
                var onceki = string.IsNullOrWhiteSpace(eslesme.AdminNotu) ? "" : eslesme.AdminNotu + Environment.NewLine;
                eslesme.AdminNotu =
                    $"{onceki}[{DateTime.Now:dd.MM.yyyy HH:mm}] (Teslim) {adminNotu.Trim()}";
            }

            if (eslesme.KayipEsya != null)
            {
                eslesme.KayipEsya.Durum = "Teslim Edildi";
                eslesme.KayipEsya.GuncellenmeTarihi = DateTime.Now;
            }

            if (eslesme.KayipBildirimi != null)
            {
                eslesme.KayipBildirimi.Durum = "Teslim Edildi";
                eslesme.KayipBildirimi.GuncellenmeTarihi = DateTime.Now;

                if (!string.IsNullOrEmpty(eslesme.KayipBildirimi.VatandasId))
                {
                    var v = eslesme.KayipBildirimi.Vatandas;
                    var teslimAlanKisi = $"{v?.Ad} {v?.Soyad}".Trim();
                    var telefon = v?.PhoneNumber;

                    var teslimIslemiVar =
                        await _context.TeslimIslemleri.AnyAsync(x => x.EslesmeId == eslesme.Id);

                    if (!teslimIslemiVar)
                    {
                        _context.TeslimIslemleri.Add(new TeslimIslemi
                        {
                            EslesmeId = eslesme.Id,
                            TeslimEdenUserId = (
                                await _context.Users
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name)
                            )?.Id,
                            TeslimAlanKisi = teslimAlanKisi,
                            IletisimTelefonu = telefon,
                            TeslimTarihi = DateTime.Now.Date,
                            TeslimSaati = DateTime.Now.TimeOfDay,
                            TeslimYeri = "Arnavutköy Belediyesi",
                            TeslimSekli = "Şahsen",
                            ImzaOnayi = true,
                            EkNotlar = adminNotu,
                            AktifMi = true,
                            OlusturulmaTarihi = DateTime.Now
                        });
                    }

                    _context.Bildirimler.Add(new Bildirim
                    {
                        AliciUserId = eslesme.KayipBildirimi.VatandasId,
                        KayipBildirimiId = eslesme.KayipBildirimi.Id,
                        EslesmeId = eslesme.Id,
                        Baslik = "✅ Eşyanız Teslim Edildi!",
                        Icerik = $"#{eslesme.KayipBildirimi.Id} numaralı \"{eslesme.KayipBildirimi.EsyaAdi}\" başvurunuzla ilgili " +
                                 $"\"{eslesme.KayipEsya?.EsyaAdi}\" ({eslesme.KayipEsya?.Kategori?.Ad}) eşyası bugün saat " +
                                 $"{DateTime.Now:HH:mm} tarihinde teslim edilmiştir. Hayırlı olsun dileriz.",
                        Turu = BildirimTuru.TeslimOnayi,
                        OkunduMu = false,
                        AktifMi = true,
                        OlusturulmaTarihi = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Eşleşme #{id} TESLİM EDİLDİ olarak işaretlendi. " +
                "Teslim kaydı oluşturuldu ve vatandaşa bildirim gönderildi!";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> YeniEslesme(int? basvuruId, int? esyaId)
        {
            ViewBag.Basvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x => x.AktifMi)
                .OrderByDescending(x => x.BasvuruTarihi)
                .Select(x => new
                {
                    x.Id,
                    Baslik = $"#{x.Id} - {x.EsyaAdi} ({x.Kategori!.Ad})"
                })
                .ToListAsync();

            ViewBag.Esyalar = await _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x => x.AktifMi)
                .OrderByDescending(x => x.OlusturmaTarihi)
                .Select(x => new
                {
                    x.Id,
                    Baslik = $"#{x.Id} - {x.EsyaAdi} ({x.Kategori!.Ad})"
                })
                .ToListAsync();

            return View(new Eslesme
            {
                KayipBildirimiId = basvuruId ?? 0,
                KayipEsyaId = esyaId ?? 0,
                Tur = EslesmeTuru.Manuel,
                Durum = EslesmeDurumu.Beklemede,
                Skor = 100
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniEslesme(Eslesme model)
        {
            if (model.KayipBildirimiId <= 0 || model.KayipEsyaId <= 0)
            {
                ModelState.AddModelError("", "Başvuru ve Eşya seçmelisiniz.");
            }

            var varMi = await _context.Eslesmeler
                .AnyAsync(x =>
                    x.KayipBildirimiId == model.KayipBildirimiId &&
                    x.KayipEsyaId == model.KayipEsyaId);

            if (varMi)
            {
                ModelState.AddModelError("", "Bu eşleşme zaten tanımlı.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Basvurular = await _context.KayipBildirimleri
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Where(x => x.AktifMi)
                    .OrderByDescending(x => x.BasvuruTarihi)
                    .Select(x => new
                    {
                        x.Id,
                        Baslik = $"#{x.Id} - {x.EsyaAdi} ({x.Kategori!.Ad})"
                    })
                    .ToListAsync();

                ViewBag.Esyalar = await _context.KayipEsyalar
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Where(x => x.AktifMi)
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Select(x => new
                    {
                        x.Id,
                        Baslik = $"#{x.Id} - {x.EsyaAdi} ({x.Kategori!.Ad})"
                    })
                    .ToListAsync();

                return View(model);
            }

            model.Tur = EslesmeTuru.Manuel;
            model.Durum = EslesmeDurumu.Beklemede;
            model.AktifMi = true;
            model.OlusturmaTarihi = DateTime.Now;
            if (model.Skor <= 0) model.Skor = 100;

            _context.Eslesmeler.Add(model);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Manuel eşleşme başarıyla oluşturuldu. (Eşleşme #{model.Id})";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var eslesme = await _context.Eslesmeler.FindAsync(id);
            if (eslesme == null) return NotFound();

            eslesme.AktifMi = false;
            eslesme.GuncellenmeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] = $"Eşleşme #{id} silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
