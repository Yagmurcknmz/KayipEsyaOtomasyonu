using System.Text;
using KayipEsyaOtomasyonu.Models;
using KayipEsyaOtomasyonu.Services;
using KayipEsyaOtomasyonu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KayipEsyaOtomasyonu.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly SmtpSettings _smtpSettings;
        private readonly IDataProtector _passwordChangeProtector;

        private const string VerifyPurposeRegister = "register";
        private const string VerifyPurposePasswordChange = "password";
        private const string VerifyPurposeForgotPassword = "reset";
        private const string VerifyPurposeEmailChange = "emailchange";
        private const string PasswordChangeTokenProvider = "PendingPasswordChange";
        private const string PasswordChangeResetTokenName = "ResetToken";
        private const string PasswordChangeNewPasswordName = "NewPassword";
        private const string PasswordChangeExpiresAtName = "ExpiresAt";
        private const string ForgotPasswordTokenProvider = "PendingForgotPassword";
        private const string ForgotPasswordResetTokenName = "ResetToken";
        private const string ForgotPasswordExpiresAtName = "ExpiresAt";
        private const string EmailChangeTokenProvider = "PendingEmailChange";
        private const string EmailChangeTokenName = "ChangeToken";
        private const string EmailChangeNewEmailName = "NewEmail";
        private const string EmailChangeOldEmailName = "OldEmail";
        private const string EmailChangeExpiresAtName = "ExpiresAt";

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment,
            IOptions<SmtpSettings> smtpOptions)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _environment = environment;
            _smtpSettings = smtpOptions.Value;
            _passwordChangeProtector =
                dataProtectionProvider.CreateProtector("AccountController.PendingPasswordChange.v1");
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
                ViewBag.ResendEmail = kullanici.Email ?? email;
                ModelState.AddModelError(
                    string.Empty,
                    "Bu hesap için e-posta doğrulaması henüz tamamlanmamış. Lütfen e-posta adresinize gönderilen kodu doğrulayın.");
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
                if (!mevcutKullanici.EmailConfirmed)
                {
                    await KayitDogrulamaKoduGonderAsync(mevcutKullanici);
                    TempData["BasariliMesaj"] =
                        "Bu e-posta adresi için doğrulama bekleyen bir hesap zaten mevcut. Yeni doğrulama kodu e-posta adresinize tekrar gönderildi.";
                    return RedirectToAction(
                        nameof(VerifyEmailCode),
                        new { userId = mevcutKullanici.Id, purpose = VerifyPurposeRegister });
                }

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
                await KayitDogrulamaKoduGonderAsync(vatandas);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "Hesabınız oluşturuldu ancak doğrulama kodu gönderilemedi: " + ex.Message;
                return RedirectToAction(
                    nameof(VerifyEmailCode),
                    new { userId = vatandas.Id, purpose = VerifyPurposeRegister });
            }

            TempData["BasariliMesaj"] =
                $"Hesabınız oluşturuldu. <strong>{vatandas.Email}</strong> adresine gönderilen doğrulama kodunu girerek kaydınızı tamamlayın.";

            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = vatandas.Id, purpose = VerifyPurposeRegister });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailCode(string userId, string purpose = VerifyPurposeRegister)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["HataMesaji"] = "Doğrulama işlemi için kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["HataMesaji"] = "Doğrulama yapılacak kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            if (string.Equals(purpose, VerifyPurposePasswordChange, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(purpose, VerifyPurposeEmailChange, StringComparison.OrdinalIgnoreCase))
            {
                var mevcutUser = await _userManager.GetUserAsync(User);
                if (mevcutUser == null || !string.Equals(mevcutUser.Id, user.Id, StringComparison.Ordinal))
                {
                    TempData["HataMesaji"] = "Bu doğrulama ekranına erişmek için hesabınızla giriş yapmış olmalısınız.";
                    return RedirectToAction(nameof(Login));
                }
            }

            var vm = new EmailKodDogrulamaViewModel
            {
                UserId = user.Id,
                Purpose = purpose,
                Email = user.Email ?? string.Empty
            };

            if (string.Equals(purpose, VerifyPurposeEmailChange, StringComparison.OrdinalIgnoreCase))
            {
                var newEmailProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeNewEmailName);

                if (!string.IsNullOrWhiteSpace(newEmailProtected))
                {
                    try
                    {
                        vm.Email = _passwordChangeProtector.Unprotect(newEmailProtected);
                    }
                    catch
                    {
                    }
                }
            }

            VerifyCodeViewAyarla(purpose);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailCode(EmailKodDogrulamaViewModel model)
        {
            if (model == null) return BadRequest();

            model.Purpose = (model.Purpose ?? VerifyPurposeRegister).Trim().ToLowerInvariant();
            model.Code = (model.Code ?? string.Empty).Trim().Replace(" ", string.Empty);
            model.Email = (model.Email ?? string.Empty).Trim();

            if (!ModelState.IsValid)
            {
                VerifyCodeViewAyarla(model.Purpose);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["HataMesaji"] = "Doğrulama yapılacak kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            if (string.Equals(model.Purpose, VerifyPurposePasswordChange, StringComparison.Ordinal))
            {
                var mevcutUser = await _userManager.GetUserAsync(User);
                if (mevcutUser == null || !string.Equals(mevcutUser.Id, user.Id, StringComparison.Ordinal))
                {
                    TempData["HataMesaji"] = "Şifre değişikliği doğrulaması için hesabınızla tekrar giriş yapın.";
                    return RedirectToAction(nameof(Login));
                }

                var kodGecerliMi = await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    model.Code);

                if (!kodGecerliMi)
                {
                    ModelState.AddModelError(string.Empty, "Doğrulama kodu hatalı veya süresi dolmuş.");
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                var resetTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeResetTokenName);

                var newPasswordProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeNewPasswordName);

                var expiresAtRaw = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeExpiresAtName);

                if (string.IsNullOrWhiteSpace(resetTokenProtected) ||
                    string.IsNullOrWhiteSpace(newPasswordProtected) ||
                    string.IsNullOrWhiteSpace(expiresAtRaw) ||
                    !DateTimeOffset.TryParse(expiresAtRaw, out var expiresAt) ||
                    expiresAt < DateTimeOffset.UtcNow)
                {
                    await TemizleBekleyenSifreDegisikligiAsync(user);
                    TempData["HataMesaji"] = "Bekleyen şifre değiştirme isteğinizin süresi dolmuş. Lütfen yeniden deneyin.";
                    return RedirectToAction(nameof(Profilim));
                }

                IdentityResult sonuc;

                try
                {
                    var resetToken = _passwordChangeProtector.Unprotect(resetTokenProtected);
                    var newPassword = _passwordChangeProtector.Unprotect(newPasswordProtected);

                    sonuc = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
                }
                catch
                {
                    await TemizleBekleyenSifreDegisikligiAsync(user);
                    TempData["HataMesaji"] = "Bekleyen şifre değiştirme verisi çözülemedi. Lütfen işlemi yeniden başlatın.";
                    return RedirectToAction(nameof(Profilim));
                }

                if (!sonuc.Succeeded)
                {
                    foreach (var err in sonuc.Errors)
                    {
                        ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
                    }
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                await TemizleBekleyenSifreDegisikligiAsync(user);
                await _signInManager.RefreshSignInAsync(user);

                try
                {
                    await SifreDegisikligiBildirimMailiGonderAsync(
                        user: user,
                        islemTuru: "ŞİFRE DEĞIŞIKLIĞI",
                        aciklama: "Şifreniz, e-posta doğrulama kodu ile onaylanarak güncellenmiştir.");
                }
                catch
                {
                }

                TempData["BasariliMesaj"] =
                    "Doğrulama tamamlandı. Şifreniz başarıyla değiştirildi.";
                return RedirectToAction(nameof(Profilim));
            }

            if (string.Equals(model.Purpose, VerifyPurposeForgotPassword, StringComparison.Ordinal))
            {
                var kodGecerliMi = await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    model.Code);

                if (!kodGecerliMi)
                {
                    ModelState.AddModelError(string.Empty, "Doğrulama kodu hatalı veya süresi dolmuş.");
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                var resetTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    ForgotPasswordTokenProvider,
                    ForgotPasswordResetTokenName);

                var expiresAtRaw = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    ForgotPasswordTokenProvider,
                    ForgotPasswordExpiresAtName);

                if (string.IsNullOrWhiteSpace(resetTokenProtected) ||
                    string.IsNullOrWhiteSpace(expiresAtRaw) ||
                    !DateTimeOffset.TryParse(expiresAtRaw, out var expiresAt) ||
                    expiresAt < DateTimeOffset.UtcNow)
                {
                    await TemizleBekleyenSifreSifirlamaAsync(user);
                    TempData["HataMesaji"] = "Şifre sıfırlama kodunuzun süresi dolmuş. Lütfen yeniden talep oluşturun.";
                    return RedirectToAction(nameof(ForgotPassword));
                }

                string resetToken;
                try
                {
                    resetToken = _passwordChangeProtector.Unprotect(resetTokenProtected);
                }
                catch
                {
                    await TemizleBekleyenSifreSifirlamaAsync(user);
                    TempData["HataMesaji"] = "Şifre sıfırlama verisi çözülemedi. Lütfen işlemi yeniden başlatın.";
                    return RedirectToAction(nameof(ForgotPassword));
                }

                TempData["BasariliMesaj"] = "Kod doğrulandı. Şimdi yeni şifrenizi belirleyin.";
                return RedirectToAction(
                    nameof(ResetPassword),
                    new { email = user.Email, token = resetToken });
            }

            if (string.Equals(model.Purpose, VerifyPurposeEmailChange, StringComparison.Ordinal))
            {
                var mevcutUser = await _userManager.GetUserAsync(User);
                if (mevcutUser == null || !string.Equals(mevcutUser.Id, user.Id, StringComparison.Ordinal))
                {
                    TempData["HataMesaji"] = "E-posta değişikliği doğrulaması için hesabınızla tekrar giriş yapın.";
                    return RedirectToAction(nameof(Login));
                }

                var kodGecerliMi = await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    model.Code);

                if (!kodGecerliMi)
                {
                    ModelState.AddModelError(string.Empty, "Doğrulama kodu hatalı veya süresi dolmuş.");
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                var changeTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeTokenName);

                var newEmailProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeNewEmailName);

                var oldEmailProtected = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeOldEmailName);

                var expiresAtRaw = await _userManager.GetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeExpiresAtName);

                if (string.IsNullOrWhiteSpace(changeTokenProtected) ||
                    string.IsNullOrWhiteSpace(newEmailProtected) ||
                    string.IsNullOrWhiteSpace(expiresAtRaw) ||
                    !DateTimeOffset.TryParse(expiresAtRaw, out var expiresAt) ||
                    expiresAt < DateTimeOffset.UtcNow)
                {
                    await TemizleBekleyenEmailDegisikligiAsync(user);
                    TempData["HataMesaji"] = "Bekleyen e-posta değiştirme isteğinizin süresi dolmuş. Lütfen yeniden deneyin.";
                    return RedirectToAction(nameof(Profilim));
                }

                string changeToken;
                string yeniEmail;
                string? eskiEmail = null;

                try
                {
                    changeToken = _passwordChangeProtector.Unprotect(changeTokenProtected);
                    yeniEmail = _passwordChangeProtector.Unprotect(newEmailProtected);

                    if (!string.IsNullOrWhiteSpace(oldEmailProtected))
                    {
                        eskiEmail = _passwordChangeProtector.Unprotect(oldEmailProtected);
                    }
                }
                catch
                {
                    await TemizleBekleyenEmailDegisikligiAsync(user);
                    TempData["HataMesaji"] = "Bekleyen e-posta değiştirme verisi çözülemedi. Lütfen işlemi yeniden başlatın.";
                    return RedirectToAction(nameof(Profilim));
                }

                var sonuc = await _userManager.ChangeEmailAsync(user, yeniEmail, changeToken);
                if (!sonuc.Succeeded)
                {
                    foreach (var err in sonuc.Errors)
                    {
                        ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
                    }
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                var userNameSonuc = await _userManager.SetUserNameAsync(user, yeniEmail);
                if (!userNameSonuc.Succeeded)
                {
                    foreach (var err in userNameSonuc.Errors)
                    {
                        ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
                    }
                    VerifyCodeViewAyarla(model.Purpose);
                    return View(model);
                }

                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                await TemizleBekleyenEmailDegisikligiAsync(user);
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
                               $"<li><strong>Önceki:</strong> <code>{eskiEmail ?? "-"}</code></li>" +
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
                }
                catch
                {
                }

                TempData["BasariliMesaj"] =
                    $"E-posta adresiniz başarıyla güncellendi. Artık <strong>{yeniEmail}</strong> adresiyle giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Profilim));
            }

            var emailConfirmResult = await _userManager.ConfirmEmailAsync(user, model.Code);
            if (!emailConfirmResult.Succeeded)
            {
                foreach (var err in emailConfirmResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, IdentityHatasiniTurkcelestir(err.Code));
                }

                if (!emailConfirmResult.Errors.Any())
                {
                    ModelState.AddModelError(string.Empty, "Doğrulama kodu hatalı veya süresi dolmuş.");
                }

                VerifyCodeViewAyarla(model.Purpose);
                return View(model);
            }

            TempData["BasariliMesaj"] =
                "E-posta doğrulamanız başarıyla tamamlandı. Artık hesabınızla giriş yapabilirsiniz.";
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
                await SifreSifirlamaKoduGonderAsync(user);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] = "Şifre sıfırlama kodu gönderilemedi: " + ex.Message;
                return View(model);
            }

            TempData["BasariliMesaj"] =
                "Şifre sıfırlama doğrulama kodu e-posta adresinize gönderildi. Gelen kutunuzu ve istenmeyen / spam klasörünü kontrol ediniz.";
            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposeForgotPassword });
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
                await TemizleBekleyenSifreSifirlamaAsync(user);

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

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["HataMesaji"] = "Hesabınıza tanımlı bir e-posta adresi bulunmadan şifre doğrulama kodu gönderilemez.";
                return RedirectToAction(nameof(Profilim));
            }

            if (!user.EmailConfirmed)
            {
                TempData["HataMesaji"] =
                    "Şifre değiştirme işlemi için önce e-posta adresinizi doğrulamış olmanız gerekir.";
                return RedirectToAction(nameof(Profilim));
            }

            try
            {
                var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeResetTokenName,
                    _passwordChangeProtector.Protect(resetToken));

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeNewPasswordName,
                    _passwordChangeProtector.Protect(model.YeniSifre));

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeExpiresAtName,
                    expiresAt.ToString("O"));

                if (DevelopmentCodeFallbackAktifMi())
                {
                    GelistirmeDogrulamaKodunuHazirla(
                        verificationCode,
                        user.Email,
                        VerifyPurposePasswordChange);
                    TempData["BasariliMesaj"] =
                        "Geliştirme ortamında doğrulama kodu ekranda gösterildi. Kodu girince şifreniz güncellenecek.";
                    return RedirectToAction(
                        nameof(VerifyEmailCode),
                        new { userId = user.Id, purpose = VerifyPurposePasswordChange });
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
                var tarihSaat = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                var html = EmailSablonuOlustur(
                    baslik: "Şifre Değiştirme Doğrulama Kodu",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Hesabınız için bir şifre değiştirme talebi oluşturuldu.</p>" +
                           $"<p>Aşağıdaki doğrulama kodunu girerek işlemi tamamlayın:</p>" +
                           $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                           $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                           $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{verificationCode}</div>" +
                           $"</div>" +
                           $"<p><strong>Geçerlilik:</strong> 10 dakika</p>" +
                           $"<p style=\"color:#64748b;font-size:12px;\">IP: {ip} | Talep Zamanı: {tarihSaat}</p>");

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "[Kayıp Eşya] Şifre Değiştirme Doğrulama Kodu",
                    html);
            }
            catch (Exception ex)
            {
                await TemizleBekleyenSifreDegisikligiAsync(user);
                TempData["HataMesaji"] =
                    "Şifre değiştirme doğrulama kodu gönderilemedi: " + ex.Message;
                return RedirectToAction(nameof(Profilim));
            }

            TempData["BasariliMesaj"] =
                $"Şifre değiştirme doğrulama kodu <strong>{user.Email}</strong> adresine gönderildi. Kod doğrulandıktan sonra şifreniz güncellenecek.";
            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposePasswordChange });
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
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
                var tarihSaat = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeTokenName,
                    _passwordChangeProtector.Protect(token));

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeNewEmailName,
                    _passwordChangeProtector.Protect(yeniEmail));

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeOldEmailName,
                    _passwordChangeProtector.Protect(user.Email ?? string.Empty));

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeExpiresAtName,
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"));

                var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);

                if (DevelopmentCodeFallbackAktifMi())
                {
                    GelistirmeDogrulamaKodunuHazirla(
                        verificationCode,
                        yeniEmail,
                        VerifyPurposeEmailChange);
                    TempData["BasariliMesaj"] =
                        $"Geliştirme ortamında doğrulama kodu ekranda gösterildi. Kodu doğruladığınızda e-posta adresiniz <strong>{yeniEmail}</strong> olarak güncellenecek.";
                    return RedirectToAction(
                        nameof(VerifyEmailCode),
                        new { userId = user.Id, purpose = VerifyPurposeEmailChange });
                }

                var yeniMailHtml = EmailSablonuOlustur(
                    baslik: "Yeni E-posta Adresi Doğrulama Kodu",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Kayıp Eşya Sistemi hesabınızın e-posta adresini <strong>bu adrese ({yeniEmail})</strong> değiştirmek istediğinize dair bir talep aldık.</p>" +
                           $"<p>Aşağıdaki doğrulama kodunu girerek e-posta değişikliğini tamamlayın:</p>" +
                           $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                           $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                           $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{verificationCode}</div>" +
                           $"</div>" +
                           $"<p><strong>Önemli:</strong> Kod doğrulandıktan sonra hesabınızın giriş adresi ve e-postası otomatik olarak <code>{yeniEmail}</code> olacaktır.</p>" +
                           $"<hr style=\"border:0;border-top:1px dashed #cbd5e1;\" />" +
                           $"<p style=\"color:#64748b;font-size:12px;\">" +
                           $"<strong>Talep Detayları:</strong><br>" +
                           $"• Eski E-posta: <code>{user.Email}</code><br>" +
                           $"• Yeni E-posta: <code>{yeniEmail}</code><br>" +
                           $"• IP: <code>{ip}</code><br>" +
                           $"• Tarih: {tarihSaat}<br>" +
                           $"• Geçerlilik: 10 dakika</p>");

                await _emailSender.SendEmailAsync(
                    yeniEmail,
                    "[Kayıp Eşya] Yeni E-posta Doğrulama Kodu",
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
                    $"E-posta değişikliği talebiniz alındı. Lütfen yeni adresiniz olan <strong>{yeniEmail}</strong> adresine gönderilen doğrulama kodunu girin. (Spam / istenmeyen klasörünü kontrol edin.) " +
                    $"Güvenliğiniz için kayıtlı eski adresinize de bir bildirim atıldı.";
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "E-posta doğrulama kodu gönderilirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposeEmailChange });
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
                TempData["BasariliMesaj"] = "Bu e-posta zaten doğrulanmış. Giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                await KayitDogrulamaKoduGonderAsync(user);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] =
                    "Doğrulama kodu gönderilemedi: " + ex.Message;
                return RedirectToAction(nameof(Login));
            }

            TempData["BasariliMesaj"] =
                "Yeni doğrulama kodu gönderildi. Lütfen gelen kutunuzu kontrol edin.";
            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposeRegister });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResendForgotPasswordCode(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["HataMesaji"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.AktifMi)
            {
                TempData["HataMesaji"] = "Şifre sıfırlama isteği için kullanıcı bulunamadı.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var resetTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                ForgotPasswordTokenProvider,
                ForgotPasswordResetTokenName);

            if (string.IsNullOrWhiteSpace(resetTokenProtected))
            {
                TempData["HataMesaji"] = "Bekleyen bir şifre sıfırlama isteği bulunamadı. Lütfen yeniden deneyin.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            try
            {
                await SifreSifirlamaKoduGonderAsync(user, resetTokenProtected);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] = "Yeni doğrulama kodu gönderilemedi: " + ex.Message;
            }

            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposeForgotPassword });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResendPasswordChangeCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var resetTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                PasswordChangeTokenProvider,
                PasswordChangeResetTokenName);

            var newPasswordProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                PasswordChangeTokenProvider,
                PasswordChangeNewPasswordName);

            if (string.IsNullOrWhiteSpace(resetTokenProtected) ||
                string.IsNullOrWhiteSpace(newPasswordProtected))
            {
                TempData["HataMesaji"] =
                    "Bekleyen bir şifre değiştirme isteği bulunamadı. Lütfen işlemi yeniden başlatın.";
                return RedirectToAction(nameof(Profilim));
            }

            try
            {
                var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    PasswordChangeTokenProvider,
                    PasswordChangeExpiresAtName,
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"));

                if (DevelopmentCodeFallbackAktifMi())
                {
                    GelistirmeDogrulamaKodunuHazirla(
                        verificationCode,
                        user.Email ?? string.Empty,
                        VerifyPurposePasswordChange);
                    TempData["BasariliMesaj"] = "Yeni doğrulama kodu geliştirme ekranında gösterildi.";
                    return RedirectToAction(
                        nameof(VerifyEmailCode),
                        new { userId = user.Id, purpose = VerifyPurposePasswordChange });
                }

                var html = EmailSablonuOlustur(
                    baslik: "Şifre Değiştirme Doğrulama Kodu (Tekrar)",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Şifre değiştirme işlemi için yeni bir doğrulama kodu talep ettiniz.</p>" +
                           $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                           $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                           $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{verificationCode}</div>" +
                           $"</div>" +
                           $"<p><strong>Geçerlilik:</strong> 10 dakika</p>");

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "[Kayıp Eşya] Şifre Değiştirme Doğrulama Kodu (Tekrar)",
                    html);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] = "Yeni doğrulama kodu gönderilemedi: " + ex.Message;
                return RedirectToAction(
                    nameof(VerifyEmailCode),
                    new { userId = user.Id, purpose = VerifyPurposePasswordChange });
            }

            TempData["BasariliMesaj"] =
                "Yeni doğrulama kodu e-posta adresinize tekrar gönderildi.";
            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposePasswordChange });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResendEmailChangeCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var newEmailProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeNewEmailName);

            var oldEmailProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeOldEmailName);

            var changeTokenProtected = await _userManager.GetAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeTokenName);

            if (string.IsNullOrWhiteSpace(newEmailProtected) ||
                string.IsNullOrWhiteSpace(changeTokenProtected))
            {
                TempData["HataMesaji"] =
                    "Bekleyen bir e-posta değiştirme isteği bulunamadı. Lütfen işlemi yeniden başlatın.";
                return RedirectToAction(nameof(Profilim));
            }

            try
            {
                var yeniEmail = _passwordChangeProtector.Unprotect(newEmailProtected);
                string? eskiEmail = null;

                if (!string.IsNullOrWhiteSpace(oldEmailProtected))
                {
                    eskiEmail = _passwordChangeProtector.Unprotect(oldEmailProtected);
                }

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    EmailChangeTokenProvider,
                    EmailChangeExpiresAtName,
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"));

                var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);

                if (DevelopmentCodeFallbackAktifMi())
                {
                    GelistirmeDogrulamaKodunuHazirla(
                        verificationCode,
                        yeniEmail,
                        VerifyPurposeEmailChange);
                    TempData["BasariliMesaj"] = "Yeni doğrulama kodu geliştirme ekranında gösterildi.";
                    return RedirectToAction(
                        nameof(VerifyEmailCode),
                        new { userId = user.Id, purpose = VerifyPurposeEmailChange });
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor";
                var tarihSaat = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

                var yeniMailHtml = EmailSablonuOlustur(
                    baslik: "Yeni E-posta Adresi Doğrulama Kodu (Tekrar)",
                    govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                           $"<p>Yeni e-posta adresinizi doğrulamak için yeni bir kod talep ettiniz.</p>" +
                           $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                           $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                           $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{verificationCode}</div>" +
                           $"</div>" +
                           $"<p><strong>Yeni E-posta:</strong> <code>{yeniEmail}</code></p>" +
                           $"<p style=\"color:#64748b;font-size:12px;\">Eski E-posta: <code>{eskiEmail ?? user.Email ?? "-"}</code><br>IP: <code>{ip}</code><br>Tarih: {tarihSaat}<br>Geçerlilik: 10 dakika</p>");

                await _emailSender.SendEmailAsync(
                    yeniEmail,
                    "[Kayıp Eşya] Yeni E-posta Doğrulama Kodu (Tekrar)",
                    yeniMailHtml);
            }
            catch (Exception ex)
            {
                TempData["HataMesaji"] = "Yeni doğrulama kodu gönderilemedi: " + ex.Message;
            }

            return RedirectToAction(
                nameof(VerifyEmailCode),
                new { userId = user.Id, purpose = VerifyPurposeEmailChange });
        }

        private void VerifyCodeViewAyarla(string purpose)
        {
            if (string.Equals(purpose, VerifyPurposePasswordChange, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PageTitle = "Şifre Değişikliği Doğrulama";
                ViewBag.PageDescription = "Mevcut e-posta adresinize gönderilen kodu girin. Kod doğrulandıktan sonra yeni şifreniz aktif olacaktır.";
                ViewBag.SubmitText = "Şifreyi Doğrula ve Güncelle";
                ViewBag.ResendAction = nameof(ResendPasswordChangeCode);
                return;
            }

            if (string.Equals(purpose, VerifyPurposeForgotPassword, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PageTitle = "Şifre Sıfırlama Kodu";
                ViewBag.PageDescription = "E-posta adresinize gönderilen kodu girin. Kod doğrulandıktan sonra yeni şifrenizi belirleyebilirsiniz.";
                ViewBag.SubmitText = "Kodu Doğrula ve Yeni Şifreye Geç";
                ViewBag.ResendAction = nameof(ResendForgotPasswordCode);
                return;
            }

            if (string.Equals(purpose, VerifyPurposeEmailChange, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PageTitle = "Yeni E-posta Doğrulama";
                ViewBag.PageDescription = "Yeni e-posta adresinize gönderilen kodu girin. Kod doğrulandıktan sonra e-posta adresiniz ve giriş adresiniz güncellenecektir.";
                ViewBag.SubmitText = "E-postayı Doğrula ve Güncelle";
                ViewBag.ResendAction = nameof(ResendEmailChangeCode);
                return;
            }

            ViewBag.PageTitle = "Kayıt E-posta Doğrulama";
            ViewBag.PageDescription = "Kayıt olurken verdiğiniz e-posta adresine gönderilen doğrulama kodunu girerek hesabınızı aktif edin.";
            ViewBag.SubmitText = "Hesabı Doğrula";
            ViewBag.ResendAction = nameof(ResendConfirmEmail);
        }

        private bool DevelopmentCodeFallbackAktifMi()
        {
            return _environment.IsDevelopment() &&
                   (string.IsNullOrWhiteSpace(_smtpSettings.Host) ||
                    string.IsNullOrWhiteSpace(_smtpSettings.Username) ||
                    string.IsNullOrWhiteSpace(_smtpSettings.Password));
        }

        private void GelistirmeDogrulamaKodunuHazirla(string code, string email, string purpose)
        {
            TempData["DevVerificationCode"] = code;
            TempData["DevVerificationPurpose"] = purpose;
            TempData["DevVerificationEmail"] = email;
            TempData["BasariliMesaj"] =
                "Geliştirme ortamında SMTP ayarı bulunmadığı için doğrulama kodu ekranda gösterildi.";
        }

        private async Task KayitDogrulamaKoduGonderAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            if (DevelopmentCodeFallbackAktifMi())
            {
                GelistirmeDogrulamaKodunuHazirla(token, user.Email ?? string.Empty, VerifyPurposeRegister);
                return;
            }

            var html = EmailSablonuOlustur(
                baslik: "Kayıt Doğrulama Kodu",
                govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                       $"<p>Kayıp Eşya Yönetim Sistemi hesabınızı aktifleştirmek için aşağıdaki doğrulama kodunu kullanın:</p>" +
                       $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                       $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                       $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{token}</div>" +
                       $"</div>" +
                       $"<p><strong>Not:</strong> Doğrulama tamamlanmadan sisteme giriş yapamazsınız.</p>");

            await _emailSender.SendEmailAsync(
                user.Email!,
                "[Kayıp Eşya] Kayıt Doğrulama Kodu",
                html);
        }

        private async Task SifreSifirlamaKoduGonderAsync(
            ApplicationUser user,
            string? mevcutProtectedResetToken = null)
        {
            var verificationCode = await _userManager.GenerateTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider);

            if (DevelopmentCodeFallbackAktifMi())
            {
                GelistirmeDogrulamaKodunuHazirla(verificationCode, user.Email ?? string.Empty, VerifyPurposeForgotPassword);
            }

            var protectedResetToken = mevcutProtectedResetToken;
            if (string.IsNullOrWhiteSpace(protectedResetToken))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                protectedResetToken = _passwordChangeProtector.Protect(resetToken);

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    ForgotPasswordTokenProvider,
                    ForgotPasswordResetTokenName,
                    protectedResetToken);
            }

            await _userManager.SetAuthenticationTokenAsync(
                user,
                ForgotPasswordTokenProvider,
                ForgotPasswordExpiresAtName,
                DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"));

            if (DevelopmentCodeFallbackAktifMi())
            {
                return;
            }

            var html = EmailSablonuOlustur(
                baslik: "Şifre Sıfırlama Doğrulama Kodu",
                govde: $"<p>Merhaba <strong>{user.Ad} {user.Soyad}</strong>,</p>" +
                       $"<p>Kayıp Eşya Yönetim Sistemi hesabınız için şifre sıfırlama talebi oluşturuldu.</p>" +
                       $"<p>Aşağıdaki doğrulama kodunu girerek yeni şifrenizi belirleme ekranına geçin:</p>" +
                       $"<div style=\"margin:24px 0;padding:18px;border-radius:14px;background:#eff6ff;border:1px dashed #0b5cff;text-align:center;\">" +
                       $"<div style=\"font-size:13px;color:#475569;margin-bottom:8px;\">Doğrulama Kodu</div>" +
                       $"<div style=\"font-size:34px;letter-spacing:8px;font-weight:800;color:#0b5cff;\">{verificationCode}</div>" +
                       $"</div>" +
                       $"<p><strong>Geçerlilik:</strong> 10 dakika</p>");

            await _emailSender.SendEmailAsync(
                user.Email!,
                "[Kayıp Eşya] Şifre Sıfırlama Doğrulama Kodu",
                html);
        }

        private async Task TemizleBekleyenSifreDegisikligiAsync(ApplicationUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                PasswordChangeTokenProvider,
                PasswordChangeResetTokenName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                PasswordChangeTokenProvider,
                PasswordChangeNewPasswordName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                PasswordChangeTokenProvider,
                PasswordChangeExpiresAtName);
        }

        private async Task TemizleBekleyenSifreSifirlamaAsync(ApplicationUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                ForgotPasswordTokenProvider,
                ForgotPasswordResetTokenName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                ForgotPasswordTokenProvider,
                ForgotPasswordExpiresAtName);
        }

        private async Task TemizleBekleyenEmailDegisikligiAsync(ApplicationUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeTokenName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeNewEmailName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeOldEmailName);

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                EmailChangeTokenProvider,
                EmailChangeExpiresAtName);
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
