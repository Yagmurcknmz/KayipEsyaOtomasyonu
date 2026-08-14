using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using FuzzySharp;

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

            // ---- SEKME SAYACLARI (Aktif tüm kayıtlar üzerinden) ----
            var tumAktif = _context.Eslesmeler.AsNoTracking().Where(x => x.AktifMi);
            ViewBag.SayacToplam = await tumAktif.CountAsync();
            ViewBag.SayacBekleyen = await tumAktif.CountAsync(x => x.Durum == EslesmeDurumu.Beklemede);
            ViewBag.SayacOnaylanan = await tumAktif.CountAsync(x => x.Durum == EslesmeDurumu.Onaylandi);
            ViewBag.SayacReddedilen = await tumAktif.CountAsync(x => x.Durum == EslesmeDurumu.Reddedildi);
            ViewBag.SayacTeslim = await tumAktif.CountAsync(x => x.Durum == EslesmeDurumu.TeslimEdildi);

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
                .Include(x => x.TeslimIslemi) // TESLIM ISLEMI DETAYINI da dahil et
                    .ThenInclude(t => t!.TeslimEdenUser)
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
                .Where(x => x.AktifMi && x.Durum != "Teslim Edildi" && x.Durum != "Sahibe Teslim Edildi")
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

                // Basvuruya en yakin 10 adet KayitEsya bul (performans icin) sonra fuzzy uygula:
                // Ilk filtre: Kategori ayni OLANLAR + Ad icinde herhangi bir kelime gecenler (genis filtre)
                var adKelimeleri = basvuruAdLower.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3).ToList();

                var potansiyelKayitlar = aktifKayitlar
                    .Where(k =>
                        // Kategori ayni ise al (en onemli filtre)
                        (basvuru.KategoriId > 0 && k.KategoriId == basvuru.KategoriId)
                        // Ya da marka ayni ise:
                        || (!string.IsNullOrEmpty(basvuruMarkaLower) && !string.IsNullOrEmpty(k.Marka) && k.Marka!.ToLowerInvariant().Contains(basvuruMarkaLower))
                        // Ya da ad anahtar kelimelerinden biri geciyorsa:
                        || (adKelimeleri.Count > 0 && !string.IsNullOrEmpty(k.EsyaAdi) && adKelimeleri.Any(kel => k.EsyaAdi!.ToLowerInvariant().Contains(kel)))
                        // Ya da renk ayni ise:
                        || (!string.IsNullOrEmpty(basvuruRenkLower) && !string.IsNullOrEmpty(k.Renk) && k.Renk!.ToLowerInvariant().Contains(basvuruRenkLower)))
                    .Take(80) // 80 kayit uzerinden fuzzy uygula (performans)
                    .ToList();

                // Her potansiyel icin FUZZY SKOR hesapla, threshold 45 ustu ise kaydet:
                foreach (var kayit in potansiyelKayitlar)
                {
                    if (mevcutSet.Contains((basvuru.Id, kayit.Id)))
                    {
                        continue;
                    }

                    (int skor, string detay) = FuzzyHelper.BasvuruEsyaBenzerligi(basvuru, kayit);

                    // Threshold: %45 ustu "eslesme olabilir" olarak isaretle (daha onceki 25'ten daha saglikli)
                    if (skor >= 45)
                    {
                        var yeni = new Eslesme
                        {
                            KayipBildirimiId = basvuru.Id,
                            KayipEsyaId = kayit.Id,
                            Tur = EslesmeTuru.Otomatik,
                            Durum = EslesmeDurumu.Beklemede,
                            Skor = Math.Clamp(skor, 0, 100),
                            EslesmeDetay = detay
                        };

                        _context.Eslesmeler.Add(yeni);
                        mevcutSet.Add((basvuru.Id, kayit.Id));
                        eklenen++;

                        if (skor >= 75)
                        {
                            // %75+ cok yuksek benzerlik: basvuruyu "Eslesme Bulundu" olarak isaretle
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
                "Vatandaşa bildirim gönderildi! Şimdi aşağıdan TESLİM ET işlemini yapabilirsiniz.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reddet(
            int id,
            string? adminNotu)
        {
            var eslesme = await _context.Eslesmeler
                .Include(x => x.KayipBildirimi)
                .Include(x => x.KayipEsya)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (eslesme == null) return NotFound();

            // (ÖNEMLİ) Eğer bu eşleşme ÖNCE ONALANDI ise, o zaman Başvuru ve Eşya durumlarını ESKİ HALİNE geri al:
            bool onaylanmisti = eslesme.Durum == EslesmeDurumu.Onaylandi;

            eslesme.Durum = EslesmeDurumu.Reddedildi;
            eslesme.IslemTarihi = DateTime.Now;
            eslesme.OnaylayanAdmin = User.Identity?.Name;
            eslesme.GuncellenmeTarihi = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(adminNotu))
            {
                var onceki = string.IsNullOrWhiteSpace(eslesme.AdminNotu) ? "" : eslesme.AdminNotu + Environment.NewLine;
                eslesme.AdminNotu =
                    $"{onceki}[{DateTime.Now:dd.MM.yyyy HH:mm}] (Ret) {adminNotu.Trim()}";
            }

            // ---- DURUM GERİ ALMA (Eşleşme Reddedildiğinde Boşa Çıkan Kayıtlar Tekrar Havuza Döner!) ----
            if (eslesme.KayipEsya != null)
            {
                if (eslesme.KayipEsya.Durum == "Eşleşme Bulundu" ||
                    eslesme.KayipEsya.Durum == "Teslim Edildi" ||
                    eslesme.KayipEsya.Durum == "Sahibe Teslim Edildi")
                {
                    eslesme.KayipEsya.Durum = "Depoda";
                    eslesme.KayipEsya.GuncellenmeTarihi = DateTime.Now;
                }
            }
            if (eslesme.KayipBildirimi != null)
            {
                if (eslesme.KayipBildirimi.Durum == "Eşleşme Bulundu" ||
                    eslesme.KayipBildirimi.Durum == "Teslim Edildi" ||
                    eslesme.KayipBildirimi.Durum == "Tamamlandı")
                {
                    eslesme.KayipBildirimi.Durum = "Eşleşme Aranıyor";
                    eslesme.KayipBildirimi.GuncellenmeTarihi = DateTime.Now;

                    // Reddedildi bildirimi gönder:
                    if (!string.IsNullOrEmpty(eslesme.KayipBildirimi.VatandasId) && onaylanmisti)
                    {
                        _context.Bildirimler.Add(new Bildirim
                        {
                            AliciUserId = eslesme.KayipBildirimi.VatandasId,
                            KayipBildirimiId = eslesme.KayipBildirimi.Id,
                            EslesmeId = eslesme.Id,
                            Baslik = "ℹ️ Eşleşme Reddedildi / Değerlendiriliyor",
                            Icerik = $"#{eslesme.KayipBildirimi.Id} numaralı \"{eslesme.KayipBildirimi.EsyaAdi}\" başvurunuz için " +
                                     $"yapılan bir eşleşme önerisi reddedildi. Yeni eşleşmeler için aramalar devam etmektedir.",
                            Turu = BildirimTuru.GenelDuyuru,
                            OkunduMu = false,
                            AktifMi = true,
                            OlusturulmaTarihi = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Eşleşme #{id} reddedildi. " +
                (onaylanmisti ? "Başvuru ve Eşya durumları ESKİ (Beklemede) haline geri alındı." : "") +
                " Başka bir eşleşme önerisi bekleniyor.";

            return RedirectToAction(nameof(Details), new { id });
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
                eslesme.KayipEsya.Durum = "Sahibe Teslim Edildi";
                eslesme.KayipEsya.GuncellenmeTarihi = DateTime.Now;
            }

            if (eslesme.KayipBildirimi != null)
            {
                eslesme.KayipBildirimi.Durum = "Tamamlandı";
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
                $"Eşleşme #{id} ✅ TAMAMLANDI: Sahibe Teslim Edildi. " +
                "Teslim kaydı ve Süreç SONA ERDİ.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> YeniEslesme(int? basvuruId, int? esyaId)
        {
            // MODERNLESTIRILDI: Artik SelectListItem degil ZENGIN nesne listesi (Resim thumbnails + Marka + Renk + Tarih)
            ViewBag.Basvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Vatandas)
                .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                .Where(x => x.AktifMi &&
                    (x.Durum == "Yeni Başvuru" || x.Durum == "İnceleniyor" || x.Durum == "Eşleşme Aranıyor"))
                .OrderByDescending(x => x.BasvuruTarihi)
                .Select(x => new
                {
                    x.Id,
                    x.EsyaAdi,
                    KategoriAd = x.Kategori != null ? x.Kategori.Ad : "-",
                    x.Marka,
                    x.Model,
                    x.Renk,
                    x.BasvuruTarihi,
                    VatandasAdSoyad = (x.Vatandas != null ? x.Vatandas.Ad + " " + x.Vatandas.Soyad : "-"),
                    BasvuruNo = x.BasvuruNo ?? ("#" + x.Id),
                    Thumbnail = (x.Resimler != null && x.Resimler.Any()
                        ? (!string.IsNullOrWhiteSpace(x.Resimler.First().ThumbnailYolu) ? ("/" + x.Resimler.First().ThumbnailYolu!.Replace("\\", "/"))
                            : (!string.IsNullOrWhiteSpace(x.Resimler.First().DosyaYolu) ? ("/" + x.Resimler.First().DosyaYolu!.Replace("\\", "/")) : null))
                        : null)
                })
                .Take(150)
                .ToListAsync();

            ViewBag.Esyalar = await _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                .Where(x => x.AktifMi &&
                    (x.Durum == "Depoda" || x.Durum == "Yeni Kayıt" || x.Durum == null || x.Durum == ""))
                .OrderByDescending(x => x.OlusturmaTarihi)
                .Select(x => new
                {
                    x.Id,
                    x.EsyaAdi,
                    KategoriAd = x.Kategori != null ? x.Kategori.Ad : "-",
                    x.Marka,
                    x.Model,
                    x.Renk,
                    x.OlusturmaTarihi,
                    x.BulunmaYeri,
                    x.RafNo,
                    Thumbnail = (x.Resimler != null && x.Resimler.Any()
                        ? (!string.IsNullOrWhiteSpace(x.Resimler.First().ThumbnailYolu) ? ("/" + x.Resimler.First().ThumbnailYolu!.Replace("\\", "/"))
                            : (!string.IsNullOrWhiteSpace(x.Resimler.First().DosyaYolu) ? ("/" + x.Resimler.First().DosyaYolu!.Replace("\\", "/")) : null))
                        : null)
                })
                .Take(150)
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
                ModelState.AddModelError("", "Lütfen sol listeden bir KAYIP BAŞVURUSU ve sağ listeden bir BULUNAN EŞYA seçin.");
            }

            var varMi = await _context.Eslesmeler
                .AnyAsync(x =>
                    x.AktifMi &&
                    x.KayipBildirimiId == model.KayipBildirimiId &&
                    x.KayipEsyaId == model.KayipEsyaId);

            if (varMi)
            {
                ModelState.AddModelError("", "Bu (Başvuru + Eşya) kombinasyonu için zaten bir eşleşme kaydı var. Eşleşmeler sayfasından görüntüleyebilirsiniz.");
            }

            if (!ModelState.IsValid)
            {
                // HATA VARSA: Tekrar aynı ZENGIN listeleri dondur
                ViewBag.Basvurular = await _context.KayipBildirimleri
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Include(x => x.Vatandas)
                    .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                    .Where(x => x.AktifMi &&
                        (x.Durum == "Yeni Başvuru" || x.Durum == "İnceleniyor" || x.Durum == "Eşleşme Aranıyor"))
                    .OrderByDescending(x => x.BasvuruTarihi)
                    .Select(x => new
                    {
                        x.Id,
                        x.EsyaAdi,
                        KategoriAd = x.Kategori != null ? x.Kategori.Ad : "-",
                        x.Marka,
                        x.Model,
                        x.Renk,
                        x.BasvuruTarihi,
                        VatandasAdSoyad = (x.Vatandas != null ? x.Vatandas.Ad + " " + x.Vatandas.Soyad : "-"),
                        BasvuruNo = x.BasvuruNo ?? ("#" + x.Id),
                        Thumbnail = (x.Resimler != null && x.Resimler.Any()
                            ? (!string.IsNullOrWhiteSpace(x.Resimler.First().ThumbnailYolu) ? ("/" + x.Resimler.First().ThumbnailYolu!.Replace("\\", "/"))
                                : (!string.IsNullOrWhiteSpace(x.Resimler.First().DosyaYolu) ? ("/" + x.Resimler.First().DosyaYolu!.Replace("\\", "/")) : null))
                            : null)
                    })
                    .Take(150)
                    .ToListAsync();

                ViewBag.Esyalar = await _context.KayipEsyalar
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Include(x => x.Resimler.Where(r => r.AktifMi && r.VarsayilanResimMi))
                    .Where(x => x.AktifMi &&
                        (x.Durum == "Depoda" || x.Durum == "Yeni Kayıt" || x.Durum == null || x.Durum == ""))
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Select(x => new
                    {
                        x.Id,
                        x.EsyaAdi,
                        KategoriAd = x.Kategori != null ? x.Kategori.Ad : "-",
                        x.Marka,
                        x.Model,
                        x.Renk,
                        x.OlusturmaTarihi,
                        x.BulunmaYeri,
                        x.RafNo,
                        Thumbnail = (x.Resimler != null && x.Resimler.Any()
                            ? (!string.IsNullOrWhiteSpace(x.Resimler.First().ThumbnailYolu) ? ("/" + x.Resimler.First().ThumbnailYolu!.Replace("\\", "/"))
                                : (!string.IsNullOrWhiteSpace(x.Resimler.First().DosyaYolu) ? ("/" + x.Resimler.First().DosyaYolu!.Replace("\\", "/")) : null))
                            : null)
                    })
                    .Take(150)
                    .ToListAsync();

                return View(model);
            }

            model.Tur = EslesmeTuru.Manuel;
            model.Durum = EslesmeDurumu.Beklemede;
            model.AktifMi = true;
            model.OlusturmaTarihi = DateTime.Now;
            if (model.Skor <= 0) model.Skor = 100;

            // Kaydetmeden ONCE: Eğer Skor 100 ve EslesmeDetayı boş ise, admin için otomatik not ekle:
            if (string.IsNullOrWhiteSpace(model.EslesmeDetay) && model.Skor >= 95)
            {
                model.EslesmeDetay = "Personel tarafından manuel eşleştirme kaydedildi. (Varsayılan Skor: %" + model.Skor + ")";
            }

            _context.Eslesmeler.Add(model);
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Manuel eşleşme başarıyla oluşturuldu. (Eşleşme #{model.Id}) " +
                "Şimdi aşağıdan ONAYLA veya ardından TESLİM ET işlemlerini yapabilirsiniz.";

            return RedirectToAction(nameof(Details), new { id = model.Id });
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
