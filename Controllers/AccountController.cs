using System.Text;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.Services;
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
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

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
            ViewBag.ResendEmail = string.Empty;
            ViewBag.ResendCallback = false;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (model == null) return BadRequest();

            ViewBag.ResendEmail = string.Empty;
            ViewBag.ResendCallback = false;

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

            if (!kullanici.EmailConfirmed)
            {
                var kullaniciRolleri = await _userManager.GetRolesAsync(kullanici);
                var yoneticiMi = kullaniciRolleri.Contains("Admin") || kullaniciRolleri.Contains("Personel");

                if (!yoneticiMi)
                {
                    var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(kullanici);
                    var callbackUrl = Url.Action(
                        nameof(ConfirmEmail),
                        "Account",
                        new { userId = kullanici.Id, token = confirmToken },
                        protocol: HttpContext.Request.Scheme);

                    ViewBag.ResendEmail = kullanici.Email ?? string.Empty;
                    ViewBag.ResendCallback = callbackUrl != null;

                    ModelState.AddModelError(
                        string.Empty,
                        "Bu e-posta adresi henüz doğrulanmamıştır. Giriş yapabilmek için lütfen e-postanıza gönderilen doğrulama linkine tıklayın. Yukarıdaki linkten tekrar doğrulama e-postası gönderin.");
                    return View(model);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "⚠️ Yönetici (Admin/Personel) hesabınız için e-posta doğrulaması atlandı (güvenlik). Lütfen mümkün olan en kısa sürede Profilim > E-posta doğrulamasını tamamlayın.");
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

            if (sonuc.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Giriş izniniz yok. Lütfen e-postanızı doğrulayın.");
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

            TempData["HataMesaji"] =
                $"Giriş başarılı ancak hesabınıza ({kullanici.Email}) henüz bir ROL atanmamış. Lütfen sistem yöneticisiyle iletişime geçin. (Mevcut Roller: {(roller.Any() ? string.Join(", ", roller) : "YOK")})";

            await _signInManager.SignOutAsync();
            TumbleCookieKillerSil(HttpContext);
            return RedirectToAction(nameof(Login));
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
            if (model == null) return BadRequest();

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
                EmailConfirmed = false,
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

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(vatandas);
                var callbackUrl = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = vatandas.Id, token },
                    protocol: HttpContext.Request.Scheme);

                var html = EmailSablonuOlustur(
                    baslik: "E-posta Adresinizi Doğrulayın",
                    govde: $"<p>Merhaba <strong>{vatandas.Ad} {vatandas.Soyad}</strong>,</p>" +
                           $"<p>Kayıp Eşya Yönetim Sistemine kayıt olduğunuz için teşekkür ederiz.</p>" +
                           $"<p>Hesabınızı doğrulamak ve giriş yapmak için aşağıdaki bağlantıya tıklayın:</p>" +
                           $"<p style=\"text-align:center;\"><a class=\"btn\" href=\"{callbackUrl}\" style=\"padding:12px 26px;background:#0b5cff;color:white;border-radius:8px;text-decoration:none;font-weight:600;\">E-postayı Doğrula ve Hesabı Aktif Et</a></p>" +
                           $"<p style=\"color:#64748b;font-size:12px;\">Bu link 24 saat süreyle geçerlidir. Bağlantıyı tıklayamıyorsanız adresi tarayıcınıza yapıştırın:<br><code>{callbackUrl}</code></p>" +
                           $"<p>Eğer bu kaydı siz oluşturmadıysanız bu e-postayı dikkate almayınız.</p>");

                await _emailSender.SendEmailAsync(
                    vatandas.Email!,
                    "Kayıp Eşya Sistemi - E-posta Doğrulama",
                    html);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "Hesabınız oluşturuldu fakat doğrulama e-postası gönderilemedi. E-posta sağlayıcı ayarlarınızı kontrol edin veya yöneticinizle iletişime geçin. Hata: " + ex.Message;
            }

            TempData["BasariliMesaj"] =
                $"Hesabınız başarıyla oluşturuldu. Giriş yapabilmek için lütfen {vatandas.Email} adresinize gönderilen doğrulama linkine tıklayın. E-posta gelmediğinde alt kısımdaki 'Doğrulama E-postasını Tekrar Gönder' bağlantısını kullanabilirsiniz.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var vm = new ConfirmEmailViewModel();

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                vm.BasariliMi = false;
                vm.Mesaj = "Geçersiz doğrulama bağlantısı.";
                vm.HataDetayi = "userId veya token parametresi boş olamaz.";
                return View(vm);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                vm.BasariliMi = false;
                vm.Mesaj = "Kullanıcı bulunamadı.";
                return View(vm);
            }

            var sonuc = await _userManager.ConfirmEmailAsync(user, token);
            if (sonuc.Succeeded)
            {
                vm.BasariliMi = true;
                vm.Mesaj = "E-posta adresiniz başarıyla doğrulandı! Artık hesabınızla giriş yapabilirsiniz.";
            }
            else
            {
                vm.BasariliMi = false;
                vm.Mesaj = "E-posta doğrulanamadı.";
                vm.HataDetayi = string.Join(" | ", sonuc.Errors.Select(x => x.Description));
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (model == null) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());

            if (user == null || !user.AktifMi)
            {
                TempData["BasariliMesaj"] =
                    "Şifre sıfırlama talimatları gönderildi. Lütfen e-postanızı kontrol edin. " +
                    "Eğer bir kayıt yoksa e-posta gönderilmeyecektir.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresi henüz doğrulanmamış. Önce e-posta doğrulaması yapınız veya hesabınız pasif ise yöneticinizle görüşün.");
                return View(model);
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = user.Email, token },
                    protocol: HttpContext.Request.Scheme);

                var html = EmailSablonuOlustur(
                    baslik: "Şifrenizi Sıfırlayın",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Kayıp Eşya Yönetim Sistemi için şifre sıfırlama talebinde bulundunuz.</p>" +
                           $"<p>Yeni şifrenizi belirlemek için aşağıdaki bağlantıya tıklayın:</p>" +
                           $"<p style=\"text-align:center;\"><a class=\"btn\" href=\"{callbackUrl}\" style=\"padding:12px 26px;background:#16a34a;color:white;border-radius:8px;text-decoration:none;font-weight:600;\">Şifreyi Sıfırla</a></p>" +
                           $"<p style=\"color:#64748b;font-size:12px;\">Bu bağlantı 2 saat süreyle geçerlidir. Eğer bu talebi siz yapmadıysanız bu e-postayı dikkate almayınız.</p>");

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Kayıp Eşya Sistemi - Şifre Sıfırlama",
                    html);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "Şifre sıfırlama e-postası gönderilemedi: " + ex.Message;
                return View(model);
            }

            TempData["BasariliMesaj"] =
                "Şifre sıfırlama talimatları e-posta adresinize gönderildi. Gelen kutunuzu ve istenmeyen / spam klasörünü kontrol ediniz.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View("Error");
            }

            var vm = new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (model == null) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                TempData["BasariliMesaj"] = "Şifreniz değiştirildi.";
                return RedirectToAction(nameof(Login));
            }

            var sonuc = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.YeniSifre);

            if (sonuc.Succeeded)
            {
                try
                {
                    await SifreDegisikligiBildirimMailiGonderAsync(
                        user: user,
                        islemTuru: "ŞİFRE SIFIRLAMA",
                        aciklama: "Şifreniz, 'Şifremi Unuttum' akışı aracılığıyla sıfırlanmıştır.");
                }
                catch
                {
                    /* Mail gönderimi başarısız olsa bile kullanıcı şifresini değiştirebilmesi için işlemi kesmiyoruz.
                       Hata SMTP logunda görünecektir. */
                }

                TempData["BasariliMesaj"] =
                    "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz. Güvenlik için şifre değişikliği e-posta adresinize bildirildi.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var err in sonuc.Errors)
            {
                ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TumbleCookieKillerSil(HttpContext);
            return RedirectToAction(nameof(Login));
        }

        private static void TumbleCookieKillerSil(HttpContext context)
        {
            if (context == null) return;

            var cookieKeys = context.Request.Cookies.Keys.ToList();
            foreach (var key in cookieKeys)
            {
                if (key.StartsWith(".AspNetCore", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("__RequestVerificationToken", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(".AspNet.Session", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("Identity", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Cookies.Delete(key);
                    context.Response.Cookies.Append(key, string.Empty, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(-1),
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = false
                    });
                }
            }

            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
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
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profilim(ProfilDuzenleViewModel model)
        {
            if (model == null) return BadRequest();

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

            TempData["BasariliMesaj"] = "Profil bilgileriniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Profilim));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SifreDegistir(SifreDegistirViewModel model)
        {
            if (model == null) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = "Lütfen tüm alanları doğru doldurun. (Şifreniz en az 6 karakter, rakam + büyük + küçük harf içermeli.)";
                return RedirectToAction(nameof(Profilim));
            }

            var sifreDogruMu = await _userManager.CheckPasswordAsync(user, model.EskiSifre);
            if (!sifreDogruMu)
            {
                TempData["HataMesaji"] = "Mevcut şifrenizi yanlış girdiniz.";
                return RedirectToAction(nameof(Profilim));
            }

            var sonuc = await _userManager.ChangePasswordAsync(
                user,
                model.EskiSifre,
                model.YeniSifre);

            if (!sonuc.Succeeded)
            {
                var msj = "Şifreniz değiştirilemedi: " +
                          string.Join(", ", sonuc.Errors.Select(x => IdentityHatasiniTurkcelestir(x.Code)));
                TempData["HataMesaji"] = msj;
                return RedirectToAction(nameof(Profilim));
            }

            await _signInManager.RefreshSignInAsync(user);

            try
            {
                await SifreDegisikligiBildirimMailiGonderAsync(
                    user: user,
                    islemTuru: "ŞİFRE DEĞIŞIKLIĞI",
                    aciklama: "Şifreniz, 'Profilim > Şifrenizi Değiştirin' bölümünden BAŞARIYLA güncellenmiştir.");
            }
            catch
            {
                // Mail gönderimi başarısız olsa bile kullanıcıyı kesintiye uğratmıyoruz.
            }

            TempData["BasariliMesaj"] = "Şifreniz başarıyla değiştirildi. Güvenlik için şifre değişikliği e-posta adresinize bildirildi.";
            return RedirectToAction(nameof(Profilim));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EmailDegistir(EmailDegistirViewModel model)
        {
            if (model == null) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["HataMesaji"] = "Lütfen tüm alanları doğru doldurun.";
                return RedirectToAction(nameof(Profilim));
            }

            var yeniEmail = model.YeniEmail.Trim().ToLowerInvariant();

            if (string.Equals(user.Email?.Trim().ToLowerInvariant(), yeniEmail, StringComparison.Ordinal))
            {
                TempData["HataMesaji"] = "Yeni e-posta adresiniz mevcut adresinizle aynı olamaz.";
                return RedirectToAction(nameof(Profilim));
            }

            var sifreDogruMu = await _userManager.CheckPasswordAsync(user, model.MevcutSifre);
            if (!sifreDogruMu)
            {
                TempData["HataMesaji"] = "Mevcut şifrenizi yanlış girdiniz. E-posta adresinizi güncelleyemezsiniz.";
                return RedirectToAction(nameof(Profilim));
            }

            var baskaKullaniciVarMi = await _userManager.FindByEmailAsync(yeniEmail);
            if (baskaKullaniciVarMi != null)
            {
                TempData["HataMesaji"] = "Bu yeni e-posta adresiyle daha önce başka bir kullanıcı kayıtlı. Farklı bir adres deneyin.";
                return RedirectToAction(nameof(Profilim));
            }

            try
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, yeniEmail);
                var callbackUrl = Url.Action(
                    nameof(ConfirmEmailChange),
                    "Account",
                    new { userId = user.Id, yeniEmail, token },
                    protocol: HttpContext.Request.Scheme);

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
                var tarihSaat = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

                var yeniMailHtml = EmailSablonuOlustur(
                    baslik: "Yeni E-posta Adresinizi Doğrulayın",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Kayıp Eşya Sistemi hesabınızın e-posta adresini <strong>bu adrese ({yeniEmail})</strong> değiştirmek istediğinize dair bir talep aldık.</p>" +
                           $"<p>Değişikliği onaylamak ve e-postanızı güncellemek için aşağıdaki bağlantıya tıklayın:</p>" +
                           $"<p style=\"text-align:center;\"><a class=\"btn\" href=\"{callbackUrl}\" style=\"padding:12px 26px;background:#0b5cff;color:white;border-radius:8px;text-decoration:none;font-weight:600;\">Yeni E-postayı Doğrula ve Güncelle</a></p>" +
                           $"<p><strong>Önemli:</strong> Bu bağlantıya tıklamanızla birlikte hesabınızın girişi (UserName) ve e-postası otomatik olarak <code>{yeniEmail}</code> olarak değiştirilecektir.</p>" +
                           $"<hr style=\"border:0;border-top:1px dashed #cbd5e1;\" />" +
                           $"<p style=\"color:#64748b;font-size:12px;\">" +
                           $"<strong>Talep Detayları:</strong><br>" +
                           $"• Eski E-posta: <code>{user.Email}</code><br>" +
                           $"• Yeni E-posta: <code>{yeniEmail}</code><br>" +
                           $"• IP: <code>{ip}</code><br>" +
                           $"• Tarih: {tarihSaat}</p>");

                await _emailSender.SendEmailAsync(
                    yeniEmail,
                    "[Kayıp Eşya] Yeni E-posta Doğrulama",
                    yeniMailHtml);

                try
                {
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        var eskiMailHtml = EmailSablonuOlustur(
                            baslik: "⚠️ Güvenlik: E-posta Değiştirme Talebi",
                            govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                                   $"<p><strong style=\"color:#dc2626;\">⚠️ Güvenlik Bildirimi</strong></p>" +
                                   $"<p>Hesabınızın e-posta adresini <code>{user.Email}</code> adresinden <code>{yeniEmail}</code> adresine değiştirmek için bir talep oluşturuldu.</p>" +
                                   $"<ul style=\"background:#fef3c7;padding:14px 18px;border-radius:10px;border-left:5px solid #f59e0b;list-style:none;\">" +
                                   $"<li><strong>Durum:</strong> <span style=\"color:#92400e;\">YENİ ADRES DOĞRULAMASI BEKLİYOR</span></li>" +
                                   $"<li><strong>IP:</strong> <code>{ip}</code></li>" +
                                   $"<li><strong>Tarih:</strong> {tarihSaat}</li>" +
                                   $"</ul>" +
                                   $"<p>❌ Bu işlemi <strong>siz yapmadıysanız</strong>: Endişelenmeyin, yeni adres doğrulanmadığı için e-postanız değişmeyecek. Şifrenizi güncellemeniz önerilir.</p>");

                        await _emailSender.SendEmailAsync(
                            user.Email,
                            "[Kayıp Eşya] Güvenlik: E-posta Değiştirme Talebi",
                            eskiMailHtml);
                    }
                }
                catch
                {
                    // Eski adrese bildirim gitmese bile kullanıcı deneyimini etkilemiyoruz.
                }

                _logger.LogInformation(
                    "[SECURITY] Email degisim talebi olusturuldu. Old={OldEmail} New={NewEmail} User={UserId} IP={Ip}",
                    user.Email, yeniEmail, user.Id, ip);

                TempData["BasariliMesaj"] =
                    $"E-posta değişikliği talebiniz alındı. Lütfen yeni adresiniz olan <strong>{yeniEmail}</strong> 'e gönderilen doğrulama bağlantısına tıklayın. (Spam / istenmeyen klasörünü kontrol edin.) " +
                    $"Güvenliğiniz için kayıtlı eski adresinize de bir bildirim atıldı.";
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "E-posta doğrulama gönderilirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Profilim));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(string userId, string yeniEmail, string token)
        {
            var vm = new ConfirmEmailViewModel();

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(yeniEmail) ||
                string.IsNullOrWhiteSpace(token))
            {
                vm.BasariliMi = false;
                vm.Mesaj = "Geçersiz e-posta değiştirme bağlantısı.";
                vm.HataDetayi = "Eksik parametre (userId / yeniEmail / token).";
                vm.DonusLinki = "/Account/Profilim";
                return View("ConfirmEmailChange", vm);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                vm.BasariliMi = false;
                vm.Mesaj = "Kullanıcı bulunamadı.";
                vm.DonusLinki = "/Account/Login";
                return View("ConfirmEmailChange", vm);
            }

            var eskiEmail = user.Email;

            var sonuc = await _userManager.ChangeEmailAsync(user, yeniEmail, token);
            if (!sonuc.Succeeded)
            {
                vm.BasariliMi = false;
                vm.Mesaj = "E-posta değiştirilemedi. Bağlantı süresi dolmuş ya da geçersiz.";
                vm.HataDetayi = string.Join(" | ", sonuc.Errors.Select(x => x.Description));
                vm.DonusLinki = "/Account/Profilim";
                return View("ConfirmEmailChange", vm);
            }

            var userNameSonuc = await _userManager.SetUserNameAsync(user, yeniEmail);
            if (!userNameSonuc.Succeeded)
            {
                vm.BasariliMi = false;
                vm.Mesaj = "E-posta güncellendi ama giriş adı (UserName) ayarlanamadı. Lütfen yöneticiyle iletişime geçin.";
                vm.HataDetayi = string.Join(" | ", userNameSonuc.Errors.Select(x => x.Description));
                vm.DonusLinki = "/Account/Profilim";
                return View("ConfirmEmailChange", vm);
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
                var tarihSaat = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

                var bildirimHtml = EmailSablonuOlustur(
                    baslik: "✅ E-posta Adresiniz Güncellendi",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Kayıp Eşya Sistemi hesabınızın e-posta adresi başarıyla güncellendi.</p>" +
                           $"<ul style=\"background:#dcfce7;padding:14px 18px;border-radius:10px;border-left:5px solid #16a34a;list-style:none;\">" +
                           $"<li><strong>Önceki:</strong> <code>{eskiEmail}</code></li>" +
                           $"<li><strong>Yeni:</strong> <code>{yeniEmail}</code></li>" +
                           $"<li><strong>IP:</strong> <code>{ip}</code></li>" +
                           $"<li><strong>Tarih:</strong> {tarihSaat}</li>" +
                           $"</ul>" +
                           $"<p>Artık sisteme <strong>YENİ</strong> e-posta adresinizle ({yeniEmail}) giriş yapacaksınız.</p>");

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "[Kayıp Eşya] E-posta Adresi Güncellendi",
                    bildirimHtml);

                if (!string.IsNullOrWhiteSpace(eskiEmail) &&
                    !string.Equals(eskiEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    await _emailSender.SendEmailAsync(
                        eskiEmail,
                        "[Kayıp Eşya] Bilgilendirme: E-posta Adresiniz Değiştirildi",
                        bildirimHtml);
                }

                _logger.LogInformation(
                    "[SECURITY] Email degisikligi tamamlandi. Old={Old} New={New} User={UserId} IP={Ip}",
                    eskiEmail, yeniEmail, user.Id, ip);
            }
            catch
            {
                // Bildirim maili başarısız olsa bile kullanıcı işlemi tamamlanmış olmalı.
            }

            vm.BasariliMi = true;
            vm.Mesaj = $"E-posta adresiniz başarıyla güncellendi! Artık <strong>{yeniEmail}</strong> adresiyle giriş yapabilirsiniz.";
            vm.DonusLinki = "/Account/Profilim";
            return View("ConfirmEmailChange", vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["HataMesaji"] = "E-posta adresi boş olamaz.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                TempData["BasariliMesaj"] = "Eğer kayıt varsa doğrulama e-postası gönderildi.";
                return RedirectToAction(nameof(Login));
            }

            if (user.EmailConfirmed)
            {
                TempData["BasariliMesaji"] = "Bu e-posta zaten doğrulanmış. Giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token },
                    protocol: HttpContext.Request.Scheme);

                var html = EmailSablonuOlustur(
                    baslik: "E-postanızı Doğrulayın (Tekrar)",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Doğrulama e-postasını tekrar istediniz. Hesabınızı doğrulamak için tıklayın:</p>" +
                           $"<p style=\"text-align:center;\"><a class=\"btn\" href=\"{callbackUrl}\" style=\"padding:12px 26px;background:#0b5cff;color:white;border-radius:8px;text-decoration:none;font-weight:600;\">E-postayı Doğrula</a></p>");

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Kayıp Eşya Sistemi - Tekrar: E-posta Doğrulama",
                    html);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "Doğrulama e-postası gönderilemedi: " + ex.Message;
                return RedirectToAction(nameof(Login));
            }

            TempData["BasariliMesaj"] =
                "Yeni doğrulama e-postası gönderildi. Lütfen gelen kutunuzu kontrol edin.";
            return RedirectToAction(nameof(Login));
        }

        private async Task SifreDegisikligiBildirimMailiGonderAsync(
            ApplicationUser user,
            string islemTuru,
            string aciklama)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(userAgent)) userAgent = "Bilinmiyor";
            if (userAgent.Length > 400) userAgent = userAgent.Substring(0, 400) + "...";

            var tarihSaat = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

            var govdeHtml = new StringBuilder()
                .Append("<p>Merhaba <strong>").Append(user.Ad).Append(' ').Append(user.Soyad).Append("</strong>,</p>")
                .Append("<p><strong style=\"color:#dc2626;\">⚠️ Güvenlik Bildirimi</strong></p>")
                .Append("<p>Hesabınızla ilgili aşağıdaki işlem gerçekleştirilmiştir:</p>")
                .Append("<ul style=\"background:#fef3c7;padding:14px 18px;border-radius:10px;border-left:5px solid #f59e0b;list-style:none;\">")
                .Append("<li><strong>İşlem Türü:</strong> <span style=\"color:#92400e;\">").Append(islemTuru).Append("</span></li>")
                .Append("<li><strong>Açıklama:</strong> ").Append(aciklama).Append("</li>")
                .Append("<li><strong>Tarih / Saat:</strong> ").Append(tarihSaat).Append("</li>")
                .Append("<li><strong>İşlem IP:</strong> <code>").Append(ip).Append("</code></li>")
                .Append("<li><strong>Cihaz / Tarayıcı:</strong> <small>").Append(userAgent).Append("</small></li>")
                .Append("</ul>")
                .Append("<p>")
                .Append("✅ Eğer bu işlemi <strong>siz yaptıysanız</strong>: Güvendeyiniz, bu e-postayı dikkate almayabilirsiniz.")
                .Append("</p>")
                .Append("<p>")
                .Append("❌ Eğer bu işlemi <strong>siz yapmadıysanız</strong>: Hemen aşağıdaki adımları uygulayın:")
                .Append("<ol>")
                .Append("<li>En kısa sürede <strong>şifrenizi tekrar değiştirin</strong> (Şifremi Unuttum akışı).</li>")
                .Append("<li>Şüpheli durumlarda <strong>yöneticinizle / sistem yöneticisiyle irtibata geçin.</strong></li>")
                .Append("<li>Hesabınızın pasif edilmesini talep edebilirsiniz.</li>")
                .Append("</ol>")
                .Append("</p>")
                .Append("<p style=\"color:#64748b;font-size:12px;\">")
                .Append("Bu e-posta, Kayıp Eşya Yönetim Sistemi tarafından otomatik olarak oluşturulmuştur. Yanıtlamayınız.")
                .Append("</p>")
                .ToString();

            var html = EmailSablonuOlustur(
                baslik: $"⚠️ Güvenlik: {islemTuru} - {tarihSaat}",
                govde: govdeHtml);

            await _emailSender.SendEmailAsync(
                user.Email!,
                $"[Kayıp Eşya] {islemTuru} Gerçekleştirildi - {tarihSaat}",
                html);

            _logger.LogInformation(
                "[SECURITY] Sifre islemi basarili. User={Email} Action={IslemTuru} IP={Ip}",
                user.Email, islemTuru, ip);
        }

        private static string EmailSablonuOlustur(string baslik, string govde)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"tr\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\" />");
            sb.AppendLine("  <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("  <title>" + baslik + "</title>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style=\"margin:0;padding:30px;background:#f1f5f9;font-family:Arial,Helvetica,sans-serif;color:#0f172a;\">");
            sb.AppendLine("  <div style=\"max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 10px 30px -10px rgba(15,23,42,0.15);\">");
            sb.AppendLine("    <div style=\"padding:28px 30px;background:linear-gradient(135deg,#2563eb 0%,#10b981 100%);color:white;\">");
            sb.AppendLine("      <div style=\"display:flex;align-items:center;gap:12px;\">");
            sb.AppendLine("        <div style=\"width:44px;height:44px;border-radius:12px;background:rgba(255,255,255,0.18);display:flex;align-items:center;justify-content:center;font-size:22px;\">🔎</div>");
            sb.AppendLine("        <div>");
            sb.AppendLine("          <div style=\"font-size:18px;font-weight:700;\">Kayıp Eşya Yönetim Sistemi</div>");
            sb.AppendLine("          <div style=\"opacity:0.92;font-size:12px;\">Arnavutköy Belediyesi</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"padding:30px;\">");
            sb.AppendLine("      <h3 style=\"margin-top:0;margin-bottom:18px;color:#0f172a;\">" + baslik + "</h3>");
            sb.AppendLine(govde);
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"padding:16px 30px;background:#f8fafc;color:#64748b;font-size:12px;border-top:1px solid #e2e8f0;\">");
            sb.AppendLine("      Bu e-posta Kayıp Eşya Yönetim Sistemi tarafından otomatik olarak gönderilmiştir. Lütfen yanıtlamayınız.");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
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

                "PasswordRequiresNonAlphanumeric" =>
                    "Şifre en az bir alfanümerik olmayan karakter içermelidir.",

                "PasswordTooShort" =>
                    "Şifre en az 6 karakter olmalıdır.",

                "PasswordMismatch" =>
                    "Mevcut şifre hatalıdır.",

                "InvalidToken" =>
                    "Doğrulama veya sıfırlama jetonu (token) geçersiz veya süresi dolmuş.",

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
