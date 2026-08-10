using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class TeslimIslemleriController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeslimIslemleriController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? ara,
            DateTime? tarihBas,
            DateTime? tarihBit)
        {
            var sorgu = _context.TeslimIslemleri
                .AsNoTracking()
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipBildirimi)
                        .ThenInclude(x => x!.Kategori)
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipBildirimi)
                        .ThenInclude(x => x!.Vatandas)
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipEsya)
                        .ThenInclude(x => x!.Kategori)
                .Include(x => x.TeslimEden)
                .Where(x => x.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(ara))
            {
                var arama = ara.Trim().ToLowerInvariant();
                sorgu = sorgu.Where(x =>
                    (x.Eslesme!.KayipBildirimi != null &&
                        (x.Eslesme.KayipBildirimi.EsyaAdi.ToLower().Contains(arama) ||
                         x.Eslesme.KayipBildirimi.Marka != null && x.Eslesme.KayipBildirimi.Marka.ToLower().Contains(arama))) ||
                    (x.Eslesme!.KayipEsya != null &&
                        x.Eslesme.KayipEsya.EsyaAdi.ToLower().Contains(arama)) ||
                    (x.TeslimAlanKisi != null && x.TeslimAlanKisi.ToLower().Contains(arama)) ||
                    (x.IletisimTelefonu != null && x.IletisimTelefonu.Contains(arama)) ||
                    (x.TcKimlikNo != null && x.TcKimlikNo.Contains(arama)));
            }

            if (tarihBas.HasValue)
            {
                sorgu = sorgu.Where(x => x.TeslimTarihi.Date >= tarihBas.Value.Date);
            }

            if (tarihBit.HasValue)
            {
                sorgu = sorgu.Where(x => x.TeslimTarihi.Date <= tarihBit.Value.Date);
            }

            var list = await sorgu
                .OrderByDescending(x => x.TeslimTarihi)
                .ThenByDescending(x => x.TeslimSaati)
                .ToListAsync();

            ViewBag.Ara = ara;
            ViewBag.TarihBas = tarihBas?.ToString("yyyy-MM-dd");
            ViewBag.TarihBit = tarihBit?.ToString("yyyy-MM-dd");

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var model = await _context.TeslimIslemleri
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipBildirimi)
                        .ThenInclude(x => x!.Kategori)
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipBildirimi)
                        .ThenInclude(x => x!.Vatandas)
                .Include(x => x.Eslesme)
                    .ThenInclude(x => x!.KayipEsya)
                        .ThenInclude(x => x!.Kategori)
                .Include(x => x.TeslimEden)
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> YeniTeslim(int? eslesmeId, int? basvuruId)
        {
            int? seciliEslesmeId = eslesmeId;

            var onaylanmisEslesmeler = await _context.Eslesmeler
                .AsNoTracking()
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Kategori)
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Vatandas)
                .Include(x => x.KayipEsya)
                    .ThenInclude(x => x!.Kategori)
                .Where(x =>
                    x.AktifMi &&
                    x.Durum == EslesmeDurumu.Onaylandi &&
                    !_context.TeslimIslemleri.Any(t => t.EslesmeId == x.Id && t.AktifMi))
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ToListAsync();

            var teslimEdenAdSoyad =
                (await _userManager.GetUserAsync(User))?.Ad + " " +
                (await _userManager.GetUserAsync(User))?.Soyad;
            ViewBag.TeslimEdenAdSoyad = teslimEdenAdSoyad.Trim();

            ViewBag.OnaylanmisEslesmeler = onaylanmisEslesmeler
                .Select(x => new
                {
                    x.Id,
                    Baslik =
                        $"#{x.Id} - Başvuru: {x.KayipBildirimi!.EsyaAdi} ({x.KayipBildirimi.Kategori!.Ad}) ↔ " +
                        $"Eşya: {x.KayipEsya!.EsyaAdi} ({x.KayipEsya.Kategori!.Ad}) - " +
                        $"{x.KayipBildirimi.Vatandas?.Ad} {x.KayipBildirimi.Vatandas?.Soyad}"
                })
                .ToList();

            var model = new TeslimIslemi
            {
                EslesmeId = seciliEslesmeId ?? 0,
                TeslimTarihi = DateTime.Now.Date,
                TeslimSaati = DateTime.Now.TimeOfDay,
                TeslimYeri = "Arnavutköy Belediyesi",
                TeslimSekli = "Şahsen",
                ImzaOnayi = true
            };

            if (seciliEslesmeId.HasValue)
            {
                var e = onaylanmisEslesmeler.FirstOrDefault(x => x.Id == seciliEslesmeId.Value);
                if (e != null)
                {
                    model.TeslimAlanKisi = $"{e.KayipBildirimi?.Vatandas?.Ad} {e.KayipBildirimi?.Vatandas?.Soyad}".Trim();
                    model.TcKimlikNo = e.KayipBildirimi?.Vatandas?.TcKimlikNo;
                    model.IletisimTelefonu = e.KayipBildirimi?.Vatandas?.PhoneNumber;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniTeslim(TeslimIslemi model)
        {
            if (model.EslesmeId <= 0)
            {
                ModelState.AddModelError(nameof(model.EslesmeId), "Bir eşleşme seçmelisiniz.");
            }

            var eslesme = await _context.Eslesmeler
                .Include(x => x.KayipBildirimi)
                    .ThenInclude(x => x!.Vatandas)
                .Include(x => x.KayipEsya)
                .FirstOrDefaultAsync(x => x.Id == model.EslesmeId);

            if (eslesme == null)
            {
                ModelState.AddModelError(nameof(model.EslesmeId), "Geçersiz eşleşme.");
            }
            else
            {
                var dahaOnceTeslim =
                    await _context.TeslimIslemleri.AnyAsync(x =>
                        x.EslesmeId == eslesme.Id && x.AktifMi);

                if (dahaOnceTeslim)
                {
                    ModelState.AddModelError("", "Bu eşleşme için zaten teslim kaydı oluşturulmuş.");
                }
            }

            if (!ModelState.IsValid)
            {
                var onaylanmisEslesmeler = await _context.Eslesmeler
                    .AsNoTracking()
                    .Include(x => x.KayipBildirimi)
                        .ThenInclude(x => x!.Kategori)
                    .Include(x => x.KayipEsya)
                        .ThenInclude(x => x!.Kategori)
                    .Where(x =>
                        x.AktifMi &&
                        x.Durum == EslesmeDurumu.Onaylandi &&
                        !_context.TeslimIslemleri.Any(t => t.EslesmeId == x.Id && t.AktifMi))
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .ToListAsync();

                ViewBag.OnaylanmisEslesmeler = onaylanmisEslesmeler
                    .Select(x => new
                    {
                        x.Id,
                        Baslik =
                            $"#{x.Id} - Başvuru: {x.KayipBildirimi!.EsyaAdi} ↔ Eşya: {x.KayipEsya!.EsyaAdi} - " +
                            $"{x.KayipBildirimi.Vatandas?.Ad} {x.KayipBildirimi.Vatandas?.Soyad}"
                    })
                    .ToList();

                var teslimEdenAdSoyad =
                    (await _userManager.GetUserAsync(User))?.Ad + " " +
                    (await _userManager.GetUserAsync(User))?.Soyad;
                ViewBag.TeslimEdenAdSoyad = teslimEdenAdSoyad.Trim();

                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            model.TeslimEdenUserId = currentUser?.Id;
            model.AktifMi = true;
            model.OlusturulmaTarihi = DateTime.Now;

            _context.TeslimIslemleri.Add(model);

            if (eslesme != null)
            {
                eslesme.Durum = EslesmeDurumu.TeslimEdildi;
                eslesme.IslemTarihi = DateTime.Now;
                eslesme.OnaylayanAdmin = User.Identity?.Name;
                eslesme.GuncellenmeTarihi = DateTime.Now;

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
                        _context.Bildirimler.Add(new Bildirim
                        {
                            AliciUserId = eslesme.KayipBildirimi.VatandasId,
                            KayipBildirimiId = eslesme.KayipBildirimi.Id,
                            EslesmeId = eslesme.Id,
                            Baslik = "✅ Eşyanız Teslim Edildi!",
                            Icerik = $"#{eslesme.KayipBildirimi.Id} numaralı \"{eslesme.KayipBildirimi.EsyaAdi}\" başvurunuzla ilgili eşyanız " +
                                     $"{model.TeslimTarihi:dd.MM.yyyy} tarihinde \"{model.TeslimYeri}\" adresinde teslim edilmiştir. Hayırlı olsun.",
                            Turu = BildirimTuru.TeslimOnayi,
                            OkunduMu = false,
                            AktifMi = true,
                            OlusturulmaTarihi = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] =
                $"Teslim kaydı başarıyla oluşturuldu (Teslim #{model.Id}). " +
                "Eşleşme, Eşya ve Başvuru durumları güncellendi, vatandaşa bildirim gönderildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var teslim = await _context.TeslimIslemleri.FindAsync(id);
            if (teslim == null) return NotFound();

            teslim.AktifMi = false;
            teslim.GuncellenmeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] = $"Teslim kaydı #{id} silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
