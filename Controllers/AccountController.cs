using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KayipEsyaOtomasyonu.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        /*
         Uygulama F5 ile kök adresten açıldığında bu action çalışır.
         Önce önceki oturumu kapatır, sonra giriş ekranını açar.
        */
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Baslangic()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var kullanici = await _userManager.FindByEmailAsync(email);

            if (kullanici == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya şifre hatalıdır.");

                return View(model);
            }

            if (!kullanici.AktifMi)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Bu kullanıcı hesabı pasif durumdadır.");

                return View(model);
            }

            var sonuc = await _signInManager.PasswordSignInAsync(
                kullanici,
                model.Sifre,
                model.BeniHatirla,
                lockoutOnFailure: true);

            if (sonuc.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Çok fazla hatalı giriş yapıldı. Hesabınız geçici olarak kilitlendi.");

                return View(model);
            }

            if (!sonuc.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya şifre hatalıdır.");

                return View(model);
            }

            var roller = await _userManager.GetRolesAsync(kullanici);

            if (roller.Contains("Vatandas"))
            {
                return RedirectToAction("Index", "Vatandas");
            }

            if (roller.Contains("Admin") ||
                roller.Contains("Personel"))
            {
                return RedirectToAction("Index", "Home");
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(AccessDenied));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new VatandasKayitViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            VatandasKayitViewModel model)
        {
            if (!model.AydinlatmaMetniOnayi)
            {
                ModelState.AddModelError(
                    nameof(model.AydinlatmaMetniOnayi),
                    "Aydınlatma metnini kabul etmelisiniz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var mevcutKullanici =
                await _userManager.FindByEmailAsync(email);

            if (mevcutKullanici != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten bulunmaktadır.");

                return View(model);
            }

            var vatandas = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = model.Telefon.Trim(),

                Ad = model.Ad.Trim(),
                Soyad = model.Soyad.Trim(),
                TcKimlikNo = model.TcKimlikNo.Trim(),
                IlceMahalle = model.IlceMahalle?.Trim(),
                Adres = model.Adres?.Trim(),

                SicilNo = null,
                Birim = null,

                AktifMi = true,
                KayitTarihi = DateTime.Now
            };

            var kullaniciSonucu = await _userManager.CreateAsync(
                vatandas,
                model.Sifre);

            if (!kullaniciSonucu.Succeeded)
            {
                foreach (var hata in kullaniciSonucu.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        IdentityHatasiniTurkcelestir(hata.Code));
                }

                return View(model);
            }

            var rolSonucu = await _userManager.AddToRoleAsync(
                vatandas,
                "Vatandas");

            if (!rolSonucu.Succeeded)
            {
                await _userManager.DeleteAsync(vatandas);

                ModelState.AddModelError(
                    string.Empty,
                    "Vatandaş rolü atanırken hata oluştu.");

                return View(model);
            }

            await _signInManager.SignInAsync(
                vatandas,
                isPersistent: false);

            TempData["BasariliMesaj"] =
                "Vatandaş hesabınız başarıyla oluşturuldu.";

            return RedirectToAction("Index", "Vatandas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profilim()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var roller = await _userManager.GetRolesAsync(user);

            var vm = new ProfilDuzenleViewModel
            {
                Ad = user.Ad,
                Soyad = user.Soyad,
                TcKimlikNo = user.TcKimlikNo,
                Telefon = user.PhoneNumber,
                Email = user.Email,
                IlceMahalle = user.IlceMahalle,
                Adres = user.Adres,
                SicilNo = user.SicilNo,
                Birim = user.Birim
            };

            ViewBag.Rol = roller.FirstOrDefault();
            ViewBag.KayitTarihi = user.KayitTarihi;

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profilim(ProfilDuzenleViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                var roller = await _userManager.GetRolesAsync(user);
                ViewBag.Rol = roller.FirstOrDefault();
                ViewBag.KayitTarihi = user.KayitTarihi;
                return View(model);
            }

            user.Ad = model.Ad.Trim();
            user.Soyad = model.Soyad.Trim();
            user.TcKimlikNo = model.TcKimlikNo?.Trim();
            user.IlceMahalle = model.IlceMahalle?.Trim();
            user.Adres = model.Adres?.Trim();
            user.PhoneNumber = model.Telefon?.Trim();

            if (!string.IsNullOrWhiteSpace(model.SicilNo))
            {
                user.SicilNo = model.SicilNo.Trim();
            }
            if (!string.IsNullOrWhiteSpace(model.Birim))
            {
                user.Birim = model.Birim.Trim();
            }

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                var roller = await _userManager.GetRolesAsync(user);
                ViewBag.Rol = roller.FirstOrDefault();
                ViewBag.KayitTarihi = user.KayitTarihi;
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.YeniSifre))
            {
                var sifreToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var sifreResult = await _userManager.ResetPasswordAsync(
                    user,
                    sifreToken,
                    model.YeniSifre);

                if (!sifreResult.Succeeded)
                {
                    foreach (var err in sifreResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
                    }
                    var roller = await _userManager.GetRolesAsync(user);
                    ViewBag.Rol = roller.FirstOrDefault();
                    ViewBag.KayitTarihi = user.KayitTarihi;
                    return View(model);
                }
            }

            TempData["BasariliMesaj"] = "Profil bilgileriniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Profilim));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static string IdentityHatasiniTurkcelestir(
            string hataKodu)
        {
            return hataKodu switch
            {
                "PasswordRequiresDigit" =>
                    "Şifre en az bir rakam içermelidir.",

                "PasswordRequiresUpper" =>
                    "Şifre en az bir büyük harf içermelidir.",

                "PasswordRequiresLower" =>
                    "Şifre en az bir küçük harf içermelidir.",

                "PasswordTooShort" =>
                    "Şifre en az 6 karakter olmalıdır.",

                "DuplicateEmail" =>
                    "Bu e-posta adresi zaten kullanılmaktadır.",

                "DuplicateUserName" =>
                    "Bu kullanıcı adı zaten kullanılmaktadır.",

                _ =>
                    "Kullanıcı hesabı oluşturulurken hata meydana geldi."
            };
        }
    }
}