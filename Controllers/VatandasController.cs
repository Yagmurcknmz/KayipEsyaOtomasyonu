using System.Security.Claims;
using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Vatandas")]
    public class VatandasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VatandasController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return View();

            ViewBag.AdSoyad = $"{user.Ad} {user.Soyad}".Trim();
            ViewBag.KayitTarihi = user.KayitTarihi;
            ViewBag.Telefon = user.PhoneNumber;
            ViewBag.TcKimlikNo = user.TcKimlikNo;

            ViewBag.OkunmamisBildirim = await _context.Bildirimler
                .CountAsync(b =>
                    b.AliciUserId == user.Id &&
                    !b.OkunduMu &&
                    b.AktifMi);

            ViewBag.BasvuruSayim = await _context.KayipBildirimleri
                .CountAsync(b =>
                    b.VatandasId == user.Id &&
                    b.AktifMi);

            ViewBag.TeslimEdilenSayim = await _context.KayipBildirimleri
                .CountAsync(b =>
                    b.VatandasId == user.Id &&
                    b.Durum == "Teslim Edildi" &&
                    b.AktifMi);

            ViewBag.EslesmeBekleyen = await _context.KayipBildirimleri
                .CountAsync(b =>
                    b.VatandasId == user.Id &&
                    (b.Durum == "İşleme Alındı" || b.Durum == "Beklemede" || b.Durum == null) &&
                    b.AktifMi);

            ViewBag.SonBildirimler = await _context.Bildirimler
                .AsNoTracking()
                .Include(b => b.KayipBildirimi)
                .Where(b => b.AliciUserId == user.Id && b.AktifMi)
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .Take(5)
                .ToListAsync();

            ViewBag.SonBasvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(b => b.Kategori)
                .Where(b => b.VatandasId == user.Id && b.AktifMi)
                .OrderByDescending(b => b.BasvuruTarihi)
                .Take(6)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Bildirimlerim()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var list = await _context.Bildirimler
                .AsNoTracking()
                .Include(b => b.KayipBildirimi)
                    .ThenInclude(b => b!.Kategori)
                .Include(b => b.Eslesme)
                .Where(b => b.AliciUserId == user.Id && b.AktifMi)
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> BildirimOku(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var bildirim = await _context.Bildirimler
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.AliciUserId == user.Id &&
                    b.AktifMi);

            if (bildirim == null) return NotFound();

            if (!bildirim.OkunduMu)
            {
                bildirim.OkunduMu = true;
                bildirim.OkunmaTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            if (bildirim.KayipBildirimiId.HasValue)
            {
                return RedirectToAction("Details", "KayipBasvuru",
                    new { id = bildirim.KayipBildirimiId.Value });
            }

            if (bildirim.EslesmeId.HasValue)
            {
                return RedirectToAction("Details", "Eslesme",
                    new { id = bildirim.EslesmeId.Value });
            }

            return RedirectToAction(nameof(Bildirimlerim));
        }

        [HttpGet]
        public async Task<IActionResult> BulunanEsyalar(
            string? aranan,
            int? kategoriId)
        {
            var vatandasId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bulunanlarSorgu = _context.KayipEsyalar
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x => x.AktifMi);

            var basvurularimSorgu = _context.KayipBildirimleri
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Where(x => x.AktifMi);

            if (!string.IsNullOrWhiteSpace(vatandasId))
            {
                basvurularimSorgu = basvurularimSorgu
                    .Where(x => x.VatandasId == vatandasId);
            }

            if (kategoriId.HasValue)
            {
                bulunanlarSorgu = bulunanlarSorgu.Where(x => x.KategoriId == kategoriId.Value);
                basvurularimSorgu = basvurularimSorgu.Where(x => x.KategoriId == kategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(aranan))
            {
                var a = aranan.Trim();
                bulunanlarSorgu = bulunanlarSorgu.Where(x =>
                    x.EsyaAdi.Contains(a) ||
                    (x.Marka != null && x.Marka.Contains(a)) ||
                    (x.Model != null && x.Model.Contains(a)) ||
                    (x.Renk != null && x.Renk.Contains(a)) ||
                    (x.BulunmaYeri != null && x.BulunmaYeri.Contains(a)) ||
                    (x.RafNo != null && x.RafNo.Contains(a)));

                basvurularimSorgu = basvurularimSorgu.Where(x =>
                    x.EsyaAdi.Contains(a) ||
                    (x.Marka != null && x.Marka.Contains(a)) ||
                    (x.Model != null && x.Model.Contains(a)) ||
                    (x.Renk != null && x.Renk.Contains(a)) ||
                    (x.KayipYeri != null && x.KayipYeri.Contains(a)) ||
                    (x.AyirtEdiciOzellik != null && x.AyirtEdiciOzellik.Contains(a)));
            }

            var bulunanlar = await bulunanlarSorgu
                .OrderByDescending(x => x.BulunmaTarihi)
                .ToListAsync();

            var basvurularim = await basvurularimSorgu
                .OrderByDescending(x => x.BasvuruTarihi)
                .ToListAsync();

            var vm = new EsyaSorgulamaViewModel
            {
                Aranan = aranan,
                KategoriId = kategoriId,
                BulunanEsyalar = bulunanlar,
                KendiKayipBildirilerim = basvurularim
            };

            ViewBag.Kategoriler = await _context.Kategoriler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            return View(vm);
        }
    }
}
