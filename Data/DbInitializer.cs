using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KayipEsyaOtomasyonu.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var context =
                serviceProvider.GetRequiredService<ApplicationDbContext>();

            await RolleriOlustur(roleManager);
            await AdminKullanicisiniOlustur(userManager);
            await HazirKategorileriOlustur(context);
        }

        public static (string Ad, string Aciklama)[] StandartKategoriler()
        {
            return new[]
            {
                (Ad: "Kimlik ve Resmî Belgeler", Aciklama: "T.C. kimlik kartı, nüfus cüzdanı, ehliyet, pasaport, seyahat belgeleri, öğrenci/kurum kimlikleri."),
                (Ad: "Kartlar",                 Aciklama: "Kredi kartı, banka kartı, ön ödeme kartı, ulaşım kartı, yakıt kartı, yemek kartı, kurum/kimlik kartları."),
                (Ad: "Cüzdan ve Para",          Aciklama: "Erkek/kadın cüzdanı, kartlık, para kesesi, kredi kartı kabı, nakit para."),
                (Ad: "Anahtar",                  Aciklama: "Ev anahtarı, araba anahtarı, kilit açma anahtarı, anahtarlık, güvenlik anahtarı."),
                (Ad: "Telefon",                  Aciklama: "Akıllı telefon, tuşlu telefon, cep telefonu aksesuarları, kılıf, şarj aleti."),
                (Ad: "Elektronik Eşya",          Aciklama: "Tablet, bilgisayar, kulaklık, AirPods, USB bellek, powerbank, şarj kablosu, akıllı saat vb. elektronik cihazlar."),
                (Ad: "Saat",                    Aciklama: "Klasik kol saati, akıllı saat, duvar saati, masa saati, spor saati."),
                (Ad: "Takı ve Değerli Eşya",     Aciklama: "Yüzük, kolye, bilezik, küpe, zincir, gerdanlık, değerli taşlar."),
                (Ad: "Çanta ve Valiz",           Aciklama: "Sırt çantası, el çantası, bavul, bez çanta, alışveriş çantası, dijital paket."),
                (Ad: "Giyim ve Aksesuar",       Aciklama: "Mont, ceket, pantolon, tişört, eldiven, şapka, atkı, bere, fular, kemer, çorap, ayakkabı, bot, terlik."),
                (Ad: "Gözlük",                   Aciklama: "Numaralı gözlük, güneş gözlüğü, okuma gözlüğü, kontakt lens, gözlük kılıfı."),
                (Ad: "Diğer",                    Aciklama: "Yukarıdaki kategorilere girmeyen diğer tüm kayıp eşyalar.")
            };
        }

        private static async Task RolleriOlustur(
            RoleManager<IdentityRole> roleManager)
        {
            string[] roller =
            {
                "Admin",
                "Personel",
                "Vatandas"
            };

            foreach (var rol in roller)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(rol));
                }
            }
        }

        private static async Task AdminKullanicisiniOlustur(
            UserManager<ApplicationUser> userManager)
        {
            const string adminEmail =
                "admin@arnavutkoy.bel.tr";

            const string adminSifre =
                "Admin123";

            var adminKullanici =
                await userManager.FindByEmailAsync(adminEmail);

            if (adminKullanici != null)
            {
                return;
            }

            adminKullanici = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Ad = "Sistem",
                Soyad = "Yöneticisi",
                Birim = "Bilgi İşlem Müdürlüğü",
                AktifMi = true,
                KayitTarihi = DateTime.Now
            };

            var sonuc = await userManager.CreateAsync(
                adminKullanici,
                adminSifre);

            if (!sonuc.Succeeded)
            {
                var hatalar = string.Join(
                    " | ",
                    sonuc.Errors.Select(x => x.Description));

                throw new Exception(
                    $"Admin kullanıcısı oluşturulamadı: {hatalar}");
            }

            await userManager.AddToRoleAsync(
                adminKullanici,
                "Admin");
        }

        private static async Task HazirKategorileriOlustur(
            ApplicationDbContext context)
        {
            var standartlar = StandartKategoriler();
            var standartAdlar = standartlar.Select(x => x.Ad).ToHashSet();
            var standartByAd = standartlar.ToDictionary(x => x.Ad, x => x);

            var isimMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Aynı anlamlı eski kategoriler -> yeni standart isim
                { "Cüzdan", "Cüzdan ve Para" },
                { "Para", "Cüzdan ve Para" },
                { "Cep Telefonu", "Telefon" },
                { "Cep Telefonları", "Telefon" },
                { "Akıllı Saat & Saatler", "Saat" },
                { "Saat ve Akıllı Saat", "Saat" },
                { "Akıllı Saat", "Saat" },
                { "Banka Kartı ve Ulaşım Kartları", "Kartlar" },
                { "Kredi Kartları", "Kartlar" },
                { "Öğrenci & Kurum & Kredi Kartları", "Kartlar" },
                { "Kimlik Kartları", "Kimlik ve Resmî Belgeler" },
                { "T.C. Kimlik Kartı", "Kimlik ve Resmî Belgeler" },
                { "Pasaport & Seyahat Belgeleri", "Kimlik ve Resmî Belgeler" },
                { "Ehliyet & Sürücü Belgesi", "Kimlik ve Resmî Belgeler" },
                { "Pasaport", "Kimlik ve Resmî Belgeler" },
                { "Bilgisayar & Tablet", "Elektronik Eşya" },
                { "Bilgisayar ve Tablet", "Elektronik Eşya" },
                { "Tablet & Bilgisayar", "Elektronik Eşya" },
                { "Kulaklık ve Elektronik Aksesuar", "Elektronik Eşya" },
                { "Kablosuz Kulaklık & Ses Cihazları", "Elektronik Eşya" },
                { "Kulak Üstü Bluetooth", "Elektronik Eşya" },
                { "Bilgisayar", "Elektronik Eşya" },
                { "Tablet", "Elektronik Eşya" },
                { "USB Bellek", "Elektronik Eşya" },
                { "Giyim Eşyası", "Giyim ve Aksesuar" },
                { "Giyim", "Giyim ve Aksesuar" },
                { "Ayakkabı & Bot", "Giyim ve Aksesuar" },
                { "Ayakkabı ve Bot", "Giyim ve Aksesuar" },
                { "Ayakkabı", "Giyim ve Aksesuar" },
                { "Takı", "Takı ve Değerli Eşya" },
                { "Takı & Mücevherat", "Takı ve Değerli Eşya" },
                { "Mücevherat", "Takı ve Değerli Eşya" },
                { "Çanta ve Bavul", "Çanta ve Valiz" },
                { "Çanta & Bavul", "Çanta ve Valiz" },
                { "Bavul", "Çanta ve Valiz" },
                { "Anahtar & Anahtarlar", "Anahtar" },
                { "Anahtar & Kilit Sistemleri", "Anahtar" },
                { "Kilit Sistemleri", "Anahtar" },
                // 12 standart dışına çıkan eski kategoriler -> Diğer
                { "Kitap ve Kırtasiye", "Diğer" },
                { "Kitap & Kırtasiye", "Diğer" },
                { "Oyuncak ve Çocuk Eşyası", "Diğer" },
                { "Bebek Ürünleri", "Diğer" },
                { "Bebek Arabası & Puset", "Diğer" },
                { "Bebek Arabası & Çocuk Ürünleri", "Diğer" },
                { "Bebek Ürünleri & Oyuncakları", "Diğer" },
                { "Spor Eşyası", "Diğer" },
                { "Spor Malzemeleri", "Diğer" },
                { "Sağlık ve Medikal Eşya", "Diğer" },
                { "Evcil Hayvan Eşyası", "Diğer" },
                { "Şemsiye & Baston", "Diğer" },
                { "Şemsiye Baston", "Diğer" },
                { "Termos Matara & Yemek Kabı", "Diğer" },
                { "Termos", "Diğer" },
                { "Ev Aletleri & Mutfak Gereci", "Diğer" },
                { "Kozmetik & Kişisel Bakım", "Diğer" },
                { "Kozmetik", "Diğer" },
                { "Müzik Aletleri & Oyun Konsolu", "Diğer" },
                { "Müzik", "Diğer" },
                { "Oyuncak", "Diğer" },
                { "Plaj & Kamp Malzemeleri", "Diğer" },
                { "Bisiklet Motorsiklet & Scooter", "Diğer" },
                { "Bisiklet/Motorsiklet", "Diğer" },
                { "Evrak Zarfı & Klasör", "Diğer" },
                { "Evrak Zarfı", "Diğer" },
            };

            // ADI degistirilecek kategorileri once bul (kullanilanlar da dahil)
            var tumKategoriler = await context.Kategoriler.ToListAsync();
            bool degisiklikVarMi = false;

            foreach (var kategori in tumKategoriler)
            {
                if (standartAdlar.Contains(kategori.Ad))
                {
                    // zaten standart
                    var std = standartByAd[kategori.Ad];
                    if (kategori.Aciklama != std.Aciklama || !kategori.AktifMi)
                    {
                        kategori.Aciklama = std.Aciklama;
                        kategori.AktifMi = true;
                        degisiklikVarMi = true;
                    }
                    continue;
                }

                // mapping listesinde var mi?
                if (isimMap.TryGetValue(kategori.Ad, out var yeniAd) &&
                    standartAdlar.Contains(yeniAd))
                {
                    // Eger hedef kategori yok ise: SU ANKI kategorinin ADINI guncelle
                    var hedefVar = tumKategoriler.Any(x => x.Ad == yeniAd);
                    if (!hedefVar)
                    {
                        kategori.Ad = yeniAd;
                        kategori.Aciklama = standartByAd[yeniAd].Aciklama;
                        kategori.AktifMi = true;
                        degisiklikVarMi = true;
                    }
                }
            }

            if (degisiklikVarMi)
            {
                await context.SaveChangesAsync();
                tumKategoriler = await context.Kategoriler.ToListAsync();
            }

            var esyaKategoriIdleri =
                await context.KayipEsyalar
                    .Select(x => x.KategoriId)
                    .Distinct()
                    .ToListAsync();

            var bildirimKategoriIdleri =
                await context.KayipBildirimleri
                    .Select(x => x.KategoriId)
                    .Distinct()
                    .ToListAsync();

            var kullanilanKategoriIdleri =
                esyaKategoriIdleri
                    .Concat(bildirimKategoriIdleri)
                    .Distinct()
                    .ToList();

            var standartKategoriIdleri =
                tumKategoriler
                    .Where(x => standartAdlar.Contains(x.Ad))
                    .Select(x => x.Id)
                    .ToList();

            var silinecekler =
                tumKategoriler
                    .Where(x =>
                        !kullanilanKategoriIdleri.Contains(x.Id) &&
                        !standartKategoriIdleri.Contains(x.Id))
                    .ToList();

            if (silinecekler.Any())
            {
                context.Kategoriler.RemoveRange(silinecekler);
                await context.SaveChangesAsync();
                tumKategoriler = await context.Kategoriler.ToListAsync();
            }

            // Son olarak eksik standartlari ekle
            foreach (var ktg in standartlar)
            {
                var mevcut = tumKategoriler.FirstOrDefault(x => x.Ad == ktg.Ad);
                if (mevcut == null)
                {
                    context.Kategoriler.Add(
                        new Kategori
                        {
                            Ad = ktg.Ad,
                            Aciklama = ktg.Aciklama,
                            AktifMi = true,
                            OlusturmaTarihi = DateTime.Now
                        });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
