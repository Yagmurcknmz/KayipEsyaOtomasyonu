using KayipEsyaOtomasyonu.Data;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KullaniciController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public KullaniciController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kullanicilar = await _userManager.Users
                .OrderByDescending(x => x.KayitTarihi)
                .ToListAsync();

            var tumRoller = await _roleManager.Roles
                .OrderBy(x => x.Name)
                .Select(x => x.Name!)
                .ToListAsync();

            ViewBag.TumRoller = tumRoller;

            var model = new List<KullaniciViewModel>();

            foreach (var kullanici in kullanicilar)
            {
                var roller = await _userManager.GetRolesAsync(kullanici);

                model.Add(new KullaniciViewModel
                {
                    Id = kullanici.Id,
                    AdSoyad = $"{kullanici.Ad} {kullanici.Soyad}",
                    Email = kullanici.Email ?? "-",
                    Telefon = kullanici.PhoneNumber,
                    TcKimlikNo = kullanici.TcKimlikNo,
                    IlceMahalle = kullanici.IlceMahalle,
                    Adres = kullanici.Adres,
                    Rol = roller.FirstOrDefault() ?? "Rol Yok",
                    Birim = kullanici.Birim,
                    SicilNo = kullanici.SicilNo,
                    AktifMi = kullanici.AktifMi,
                    KayitTarihi = kullanici.KayitTarihi
                });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult PersonelEkle()
        {
            return View(new PersonelEkleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelEkle(
            PersonelEkleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var mevcutKullanici =
                await _userManager.FindByEmailAsync(model.Email);

            if (mevcutKullanici != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");

                return View(model);
            }

            var personel = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Ad = model.Ad.Trim(),
                Soyad = model.Soyad.Trim(),
                SicilNo = model.SicilNo.Trim(),
                Birim = model.Birim.Trim(),
                AktifMi = true,
                KayitTarihi = DateTime.Now
            };

            var sonuc = await _userManager.CreateAsync(
                personel,
                model.Sifre);

            if (!sonuc.Succeeded)
            {
                foreach (var hata in sonuc.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        hata.Description);
                }

                return View(model);
            }

            var rolSonucu = await _userManager.AddToRoleAsync(
                personel,
                "Personel");

            if (!rolSonucu.Succeeded)
            {
                await _userManager.DeleteAsync(personel);

                foreach (var hata in rolSonucu.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        hata.Description);
                }

                return View(model);
            }

            TempData["BasariliMesaj"] =
                "Personel hesabı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pasiflestir(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null)
            {
                return NotFound();
            }

            var mevcutRol = (await _userManager.GetRolesAsync(kullanici)).FirstOrDefault();
            if (mevcutRol == "Admin")
            {
                var adminSayisi = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminSayisi.Count <= 1)
                {
                    TempData["HataMesaj"] =
                        "Sistemdeki tek admin hesabı pasifleştirilemez.";

                    return RedirectToAction(nameof(Index));
                }
            }

            kullanici.AktifMi = false;
            await _userManager.UpdateAsync(kullanici);

            TempData["BasariliMesaj"] =
                $"{kullanici.Ad} {kullanici.Soyad} adlı kullanıcı pasifleştirildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aktiflestir(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null)
            {
                return NotFound();
            }

            kullanici.AktifMi = true;
            await _userManager.UpdateAsync(kullanici);

            TempData["BasariliMesaj"] =
                $"{kullanici.Ad} {kullanici.Soyad} adlı kullanıcı aktifleştirildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RolDegistir(
            string id,
            string yeniRol)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(yeniRol))
            {
                return BadRequest();
            }

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null)
            {
                return NotFound();
            }

            var aktifRoller = await _userManager.GetRolesAsync(kullanici);

            if (aktifRoller.Contains("Admin"))
            {
                var adminSayisi = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminSayisi.Count <= 1 && yeniRol != "Admin")
                {
                    TempData["HataMesaj"] =
                        "Sistemdeki tek admin hesabının rolü değiştirilemez.";

                    return RedirectToAction(nameof(Index));
                }
            }

            if (aktifRoller.Any())
            {
                await _userManager.RemoveFromRolesAsync(kullanici, aktifRoller);
            }

            await _userManager.AddToRoleAsync(kullanici, yeniRol);

            TempData["BasariliMesaj"] =
                $"{kullanici.Ad} {kullanici.Soyad} adlı kullanıcının rolü " +
                $"{yeniRol} olarak güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Detay(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null) return NotFound();

            var roller = await _userManager.GetRolesAsync(kullanici);
            ViewBag.Rol = roller.FirstOrDefault();

            ViewBag.ToplamBasvuru = await _context.KayipBildirimleri
                .CountAsync(b => b.VatandasId == kullanici.Id && b.AktifMi);

            ViewBag.AktifBasvurular = await _context.KayipBildirimleri
                .AsNoTracking()
                .Include(b => b.Kategori)
                .Where(b => b.VatandasId == kullanici.Id && b.AktifMi)
                .OrderByDescending(b => b.BasvuruTarihi)
                .Take(10)
                .ToListAsync();

            ViewBag.ToplamTeslim = await _context.TeslimIslemleri
                .Include(t => t.Eslesme)
                .CountAsync(t =>
                    t.AktifMi &&
                    t.Eslesme != null &&
                    t.Eslesme.KayipBildirimi != null &&
                    t.Eslesme.KayipBildirimi.VatandasId == kullanici.Id);

            return View(kullanici);
        }

        [HttpGet]
        public async Task<IActionResult> Duzenle(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null) return NotFound();

            var roller = await _userManager.GetRolesAsync(kullanici);
            ViewBag.Rol = roller.FirstOrDefault();
            ViewBag.TumRoller = await _roleManager.Roles
                .Select(r => r.Name!).OrderBy(r => r).ToListAsync();

            var vm = new ProfilDuzenleViewModel
            {
                Ad = kullanici.Ad,
                Soyad = kullanici.Soyad,
                TcKimlikNo = kullanici.TcKimlikNo,
                Telefon = kullanici.PhoneNumber,
                Email = kullanici.Email,
                IlceMahalle = kullanici.IlceMahalle,
                Adres = kullanici.Adres,
                SicilNo = kullanici.SicilNo,
                Birim = kullanici.Birim
            };

            ViewBag.KullaniciId = kullanici.Id;
            ViewBag.KayitTarihi = kullanici.KayitTarihi;
            ViewBag.AktifMi = kullanici.AktifMi;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(string id, ProfilDuzenleViewModel model, bool? aktifMi, string? yeniRol)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var kullanici = await _userManager.FindByIdAsync(id);
            if (kullanici == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var roller = await _userManager.GetRolesAsync(kullanici);
                ViewBag.Rol = roller.FirstOrDefault();
                ViewBag.TumRoller = await _roleManager.Roles
                    .Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                ViewBag.KullaniciId = kullanici.Id;
                ViewBag.KayitTarihi = kullanici.KayitTarihi;
                ViewBag.AktifMi = kullanici.AktifMi;
                return View(model);
            }

            kullanici.Ad = model.Ad.Trim();
            kullanici.Soyad = model.Soyad.Trim();
            kullanici.TcKimlikNo = model.TcKimlikNo?.Trim();
            kullanici.IlceMahalle = model.IlceMahalle?.Trim();
            kullanici.Adres = model.Adres?.Trim();
            kullanici.PhoneNumber = model.Telefon?.Trim();
            kullanici.SicilNo = model.SicilNo?.Trim();
            kullanici.Birim = model.Birim?.Trim();
            kullanici.AktifMi = aktifMi ?? true;

            if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != kullanici.Email)
            {
                var emailToken = await _userManager.GenerateChangeEmailTokenAsync(kullanici, model.Email.Trim());
                await _userManager.ChangeEmailAsync(kullanici, model.Email.Trim(), emailToken);
                kullanici.UserName = model.Email.Trim();
            }

            var updateResult = await _userManager.UpdateAsync(kullanici);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors) ModelState.AddModelError("", err.Description);
                var roller = await _userManager.GetRolesAsync(kullanici);
                ViewBag.Rol = roller.FirstOrDefault();
                ViewBag.TumRoller = await _roleManager.Roles
                    .Select(r => r.Name!).OrderBy(r => r).ToListAsync();
                ViewBag.KullaniciId = kullanici.Id;
                ViewBag.KayitTarihi = kullanici.KayitTarihi;
                ViewBag.AktifMi = kullanici.AktifMi;
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(yeniRol))
            {
                var aktifRoller = await _userManager.GetRolesAsync(kullanici);
                if (!aktifRoller.Contains(yeniRol))
                {
                    if (aktifRoller.Contains("Admin"))
                    {
                        var adminSayisi = await _userManager.GetUsersInRoleAsync("Admin");
                        if (adminSayisi.Count <= 1 && yeniRol != "Admin")
                        {
                            TempData["HataMesaj"] = "Sistemdeki tek admin rolü değiştirilemez.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    if (aktifRoller.Any()) await _userManager.RemoveFromRolesAsync(kullanici, aktifRoller);
                    await _userManager.AddToRoleAsync(kullanici, yeniRol);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.YeniSifre))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(kullanici);
                await _userManager.ResetPasswordAsync(kullanici, token, model.YeniSifre);
            }

            TempData["BasariliMesaj"] = $"{kullanici.Ad} {kullanici.Soyad} kullanıcı bilgileri güncellendi.";
            return RedirectToAction(nameof(Detay), new { id = kullanici.Id });
        }
    }
}
