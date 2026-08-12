using System.Diagnostics;
using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin,Personel")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private static string NormRol(string rol) => (rol ?? "").Trim().ToLowerInvariant();

        public async Task<IActionResult> Index()
        {
            async Task<int> RoleCount(string r)
            {
                var nr = NormRol(r);
                var tumRoller = await _roleManager.Roles.AsNoTracking().ToListAsync();
                var rname = tumRoller.FirstOrDefault(x =>
                    NormRol(x.Name!) == nr || NormRol(x.NormalizedName!) == nr)?.Name;
                if (rname == null) return 0;
                var list = await _userManager.GetUsersInRoleAsync(rname);
                return list.Count;
            }

            int toplamVatandas = (await RoleCount("Vatandaş")) + (await RoleCount("Vatandas"));
            int toplamPersonel = await RoleCount("Personel");
            int toplamAdmin = await RoleCount("Admin");
            int toplamKullanici = await _userManager.Users.CountAsync();

            var toplamKayipEsya = await _context.KayipEsyalar
                .CountAsync();

            var model = new DashboardViewModel
            {
                ToplamKayipEsya = toplamKayipEsya,

                DepodaBekleyen = await _context.KayipEsyalar
                    .CountAsync(x =>
                        x.AktifMi &&
                        x.Durum == "Depoda"),

                TeslimEdilen = await _context.KayipEsyalar
                    .CountAsync(x =>
                        x.Durum == "Teslim Edildi"),

                AktifKayipEsya = await _context.KayipEsyalar
                    .CountAsync(x => x.AktifMi),

                ToplamVatandasBildirimi = await _context.KayipBildirimleri
                    .CountAsync(),

                AktifBasvuru = await _context.KayipBildirimleri
                    .CountAsync(x =>
                        x.AktifMi &&
                        x.Durum != "Çözüldü" &&
                        x.Durum != "Pasif"),

                ToplamKullanici = toplamKullanici,
                ToplamPersonel = toplamPersonel,
                ToplamVatandas = toplamVatandas,

                BekleyenEslesme = await _context.Eslesmeler
                    .CountAsync(e => e.AktifMi && e.Durum == EslesmeDurumu.Beklemede),

                SonKayipEsyalar = await _context.KayipEsyalar
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Take(8)
                    .ToListAsync(),

                SonBasvurular = await _context.KayipBildirimleri
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Include(x => x.Vatandas)
                    .OrderByDescending(x => x.BasvuruTarihi)
                    .Take(5)
                    .ToListAsync(),

                SonEslesmeler = await _context.Eslesmeler
                    .AsNoTracking()
                    .Include(x => x.KayipBildirimi)
                        .ThenInclude(x => x!.Vatandas)
                    .Include(x => x.KayipEsya)
                        .ThenInclude(x => x!.Kategori)
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Take(5)
                    .ToListAsync()
            };


            var durumlar = new[]
            {
                new { Ad = "Yeni Kayıt", Renk = "bg-primary" },
                new { Ad = "Depoda", Renk = "bg-warning" },
                new { Ad = "Eşleşme Bulundu", Renk = "bg-info" },
                new { Ad = "Vatandaşa Haber Verildi", Renk = "bg-secondary" },
                new { Ad = "Teslim Bekliyor", Renk = "bg-orange" },
                new { Ad = "Teslim Edildi", Renk = "bg-success" },
                new { Ad = "Arşivlendi", Renk = "bg-muted" }
            };

            foreach (var durum in durumlar)
            {
                var adet = await _context.KayipEsyalar
                    .CountAsync(x => x.Durum == durum.Ad);

                if (adet > 0)
                {
                    model.DurumBazliDagilim.Add(new DashboardDurumGrafik
                    {
                        Durum = durum.Ad,
                        Adet = adet,
                        Renk = durum.Renk
                    });
                }
            }

            // EKSTRA: Onaylanan / Reddedilen / Bugün istatistik
            model.OnaylananEslesme = await _context.Eslesmeler.CountAsync(e => e.AktifMi && e.Durum == EslesmeDurumu.Onaylandi);
            model.ReddedilenEslesme = await _context.Eslesmeler.CountAsync(e => e.AktifMi && e.Durum == EslesmeDurumu.Reddedildi);
            model.BugunYeniKayit = await _context.KayipEsyalar.CountAsync(x => x.OlusturmaTarihi.Date == DateTime.Today);
            model.BugunYeniBasvuru = await _context.KayipBildirimleri.CountAsync(x => x.BasvuruTarihi.Date == DateTime.Today);

            var topBulunan = await _context.KayipEsyalar.CountAsync(x => x.AktifMi && x.Durum == "Teslim Edildi");
            model.TeslimOraniYuzde = toplamKayipEsya == 0 ? 0 : Math.Round(100.0 * topBulunan / toplamKayipEsya, 1);


            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> TumBildirimleriOkunduYap()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index), "Home");

            var okunmamislar = await _context.Bildirimler
                .Where(b =>
                    b.AliciUserId == user.Id &&
                    !b.OkunduMu &&
                    b.AktifMi)
                .ToListAsync();

            foreach (var b in okunmamislar)
            {
                b.OkunduMu = true;
                b.OkunmaTarihi = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["BasariliMesaj"] = $"{okunmamislar.Count} bildirim okundu olarak işaretlendi.";

            if (User.IsInRole("Vatandas"))
            {
                return RedirectToAction("Bildirimlerim", "Vatandas");
            }

            if (Request.Headers["Referer"].ToString().Length > 0)
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            return RedirectToAction(nameof(Index), "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Arama(string? ara)
        {
            var vm = new ViewModels.AramaSonucuViewModel { AramaKelimesi = ara?.Trim() };

            if (!string.IsNullOrWhiteSpace(vm.AramaKelimesi))
            {
                var arama = vm.AramaKelimesi.ToLowerInvariant();

                vm.Esyalar = await _context.KayipEsyalar
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Where(x =>
                        x.AktifMi &&
                        (x.EsyaAdi.ToLower().Contains(arama) ||
                         (x.Marka != null && x.Marka.ToLower().Contains(arama)) ||
                         (x.Renk != null && x.Renk.ToLower().Contains(arama)) ||
                         (x.Aciklama != null && x.Aciklama.ToLower().Contains(arama)) ||
                         (x.Kategori != null && x.Kategori.Ad.ToLower().Contains(arama))))
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Take(20)
                    .ToListAsync();

                vm.Basvurular = await _context.KayipBildirimleri
                    .AsNoTracking()
                    .Include(x => x.Kategori)
                    .Include(x => x.Vatandas)
                    .Where(x =>
                        x.AktifMi &&
                        (x.EsyaAdi.ToLower().Contains(arama) ||
                         (x.Marka != null && x.Marka.ToLower().Contains(arama)) ||
                         (x.Renk != null && x.Renk.ToLower().Contains(arama)) ||
                         (x.Aciklama != null && x.Aciklama.ToLower().Contains(arama)) ||
                         (x.Vatandas != null &&
                          ((x.Vatandas.Ad + " " + x.Vatandas.Soyad).ToLower().Contains(arama) ||
                           (x.Vatandas.Email != null && x.Vatandas.Email.ToLower().Contains(arama))))))
                    .OrderByDescending(x => x.BasvuruTarihi)
                    .Take(20)
                    .ToListAsync();

                vm.Eslesmeler = await _context.Eslesmeler
                    .AsNoTracking()
                    .Include(x => x.KayipBildirimi).ThenInclude(x => x!.Vatandas)
                    .Include(x => x.KayipEsya).ThenInclude(x => x!.Kategori)
                    .Where(x =>
                        x.AktifMi &&
                        ((x.KayipBildirimi != null && x.KayipBildirimi.EsyaAdi.ToLower().Contains(arama)) ||
                         (x.KayipEsya != null && x.KayipEsya.EsyaAdi.ToLower().Contains(arama)) ||
                         (x.EslesmeDetay != null && x.EslesmeDetay.ToLower().Contains(arama))))
                    .OrderByDescending(x => x.OlusturmaTarihi)
                    .Take(20)
                    .ToListAsync();

                if (User.IsInRole("Admin"))
                {
                    vm.Kullanicilar = (await _userManager.Users
                        .AsNoTracking()
                        .Where(u =>
                            (u.Ad != null && u.Ad.ToLower().Contains(arama)) ||
                            (u.Soyad != null && u.Soyad.ToLower().Contains(arama)) ||
                            (u.Email != null && u.Email.ToLower().Contains(arama)) ||
                            (u.UserName != null && u.UserName.ToLower().Contains(arama)))
                        .Take(10)
                        .ToListAsync())
                        .Cast<Models.ApplicationUser>()
                        .ToList();
                }
            }

            vm.ToplamEsya = vm.Esyalar.Count;
            vm.ToplamBasvuru = vm.Basvurular.Count;
            vm.ToplamEslesme = vm.Eslesmeler.Count;
            vm.ToplamKullanici = vm.Kullanicilar.Count;

            return View(vm);
        }
    }
}
