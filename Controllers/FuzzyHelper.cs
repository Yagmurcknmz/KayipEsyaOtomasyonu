using FuzzySharp;
using KayipEsyaOtomasyonu.Models;
using System.Text;

namespace KayipEsyaOtomasyonu.Controllers
{
    /// <summary>
    /// Bulanık arama (Fuzzy Matching) islemleri icin yardimci static sinif.
    /// ESKI: Basit string.Contains (tam eslesme gerektiriyordu, "Telefon" yazinca "Cep Telefonu" gelmiyordu)
    /// YENI: Levenshtein + Jaro-Winkler tabanli FuzzySharp.Fuzz ile belirli bir eslesme orani uzerinde sonuclari dondurur.
    /// </summary>
    public static class FuzzyHelper
    {
        /// <summary>
        /// Yalnizca 2 metni karsilastirir, 0-100 arasi benzerlik dondurur.
        /// Bos veya null ise 0 doner.
        /// </summary>
        public static int MetinBenzerligi(string? kaynak, string? hedef)
        {
            if (string.IsNullOrWhiteSpace(kaynak) || string.IsNullOrWhiteSpace(hedef))
            {
                return 0;
            }

            string a = kaynak.Trim().ToLowerInvariant();
            string b = hedef.Trim().ToLowerInvariant();

            if (a == b) return 100;
            if (a.Contains(b) || b.Contains(a)) return 90;

            // Kelime sirasi fark etmezse (token) en iyi sonucu verir: "Cep Telefonu" vs "Telefon Siyah"
            int tokenSet = Fuzz.TokenSetRatio(a, b);

            // Sıfıra cok yakin ise (anlamsiz) kac tane ortak harf var diye bak (alternatif):
            if (tokenSet <= 20)
            {
                int partialRatio = Fuzz.PartialRatio(a, b);
                return Math.Max(tokenSet, partialRatio);
            }

            return tokenSet;
        }

        /// <summary>
        /// Kayıp Basvuru (Vatandas taslak) ile KayipEsya (Depoda bekleyen) esyalarini karsilastirir.
        /// SKOR 0-100 arasidir:
        /// - Kategori ESLESIRSE: +35 bonus (kesin)
        /// - Ad: %40 agirlik
        /// - Marka: %20 agirlik
        /// - Model: %15 agirlik
        /// - Renk: %10 agirlik
        /// - Ozellikler: %5 agirlik
        /// </summary>
        public static (int Skor, string Detay) BasvuruEsyaBenzerligi(KayipBildirimi basvuru, KayipEsya esya)
        {
            int toplam = 0;
            var detay = new StringBuilder();

            // --- 1. Kategori Kesin Kontrol (EN ONEMLI) ---
            if (basvuru.KategoriId > 0 && basvuru.KategoriId == esya.KategoriId)
            {
                toplam += 35;
                detay.Append("✅ Kategori eşleşmesi, ");
            }
            else if (basvuru.KategoriId > 0 && esya.KategoriId > 0)
            {
                // Farklı kategorilerdeyse ciddi ceza ver (ama yine de diger alanlar benzer ise bulunabilir):
                toplam -= 12;
                detay.Append("⚠️ Kategori farklı, ");
            }

            // --- 2. Agirlikli Fuzzy Benzerlik (Max 65 puan) ---
            int adSkor = MetinBenzerligi(basvuru.EsyaAdi, esya.EsyaAdi);
            double adKatki = adSkor * 0.40;

            int markaSkor = MetinBenzerligi(basvuru.Marka, esya.Marka);
            double markaKatki = markaSkor * 0.20;

            int modelSkor = MetinBenzerligi(basvuru.Model, esya.Model);
            double modelKatki = modelSkor * 0.15;

            int renkSkor = MetinBenzerligi(basvuru.Renk, esya.Renk);
            double renkKatki = renkSkor * 0.10;

            int ozellikSkor = MetinBenzerligi(basvuru.AyirtEdiciOzellik, esya.AyirtEdiciOzellik);
            double ozellikKatki = ozellikSkor * 0.05;

            double toplamFuzzy = adKatki + markaKatki + modelKatki + renkKatki + ozellikKatki;
            toplam += (int)Math.Round(toplamFuzzy);

            if (adSkor >= 60) detay.Append($"Ad %{adSkor} benzer, ");
            if (markaSkor >= 50) detay.Append($"Marka %{markaSkor} benzer, ");
            if (modelSkor >= 50) detay.Append($"Model %{modelSkor} benzer, ");
            if (renkSkor >= 70) detay.Append($"Renk %{renkSkor} benzer, ");
            if (ozellikSkor >= 40) detay.Append($"Özellik %{ozellikSkor} benzer, ");

            // Sinirla (0-100 arasi)
            toplam = Math.Clamp(toplam, 0, 100);

            string detayMetni = detay.ToString().Trim().TrimEnd(',').Trim();
            if (string.IsNullOrWhiteSpace(detayMetni))
            {
                detayMetni = $"Genel bulanık benzerlik skoru: %{toplam}";
            }
            else
            {
                detayMetni = $"(Toplam %{toplam}) " + detayMetni;
            }

            return (toplam, detayMetni);
        }

        /// <summary>
        /// Tek bir anahtar kelime (aranan) ile Kayıp Eşya (ESYA) nesnesinin genel benzerligini hesaplar.
        /// VatandasController.BulunanEsyalar'da arama sonuclarini SKOR'a gore SIRALAMAK icin kullanilir.
        /// </summary>
        public static int AnahtarKelimeEsyaSkoru(string? aranan, KayipEsya esya)
        {
            if (string.IsNullOrWhiteSpace(aranan))
            {
                // Arama yoksa: En yeni once gelmesi icin ID / esyaya gore yüksek dön (sıra bozulmasın)
                return 0;
            }

            // Hepsinin skorlarini topla, en yuksek alanı ver:
            int[] skorlar = new[]
            {
                MetinBenzerligi(aranan, esya.EsyaAdi),
                MetinBenzerligi(aranan, esya.Marka),
                MetinBenzerligi(aranan, esya.Model),
                MetinBenzerligi(aranan, esya.Renk),
                MetinBenzerligi(aranan, esya.Aciklama),
                MetinBenzerligi(aranan, esya.AyirtEdiciOzellik),
                MetinBenzerligi(aranan, esya.BulunmaYeri),
                MetinBenzerligi(aranan, esya.RafNo),
            };

            // En yuksek olani + digerlerinin %10'u toplamsi:
            int maxSkor = skorlar.Max();
            double bonus = skorlar.Sum(s => s) * 0.03;
            return (int)Math.Clamp(Math.Round(maxSkor + bonus), 0, 100);
        }

        /// <summary>
        /// Ayni metodun KayipBildirimi (basvuru) icin overload'i.
        /// </summary>
        public static int AnahtarKelimeBasvuruSkoru(string? aranan, KayipBildirimi basvuru)
        {
            if (string.IsNullOrWhiteSpace(aranan)) return 0;

            int[] skorlar = new[]
            {
                MetinBenzerligi(aranan, basvuru.EsyaAdi),
                MetinBenzerligi(aranan, basvuru.Marka),
                MetinBenzerligi(aranan, basvuru.Model),
                MetinBenzerligi(aranan, basvuru.Renk),
                MetinBenzerligi(aranan, basvuru.Aciklama),
                MetinBenzerligi(aranan, basvuru.AyirtEdiciOzellik),
                MetinBenzerligi(aranan, basvuru.KayipYeri),
            };

            int maxSkor = skorlar.Max();
            double bonus = skorlar.Sum(s => s) * 0.03;
            return (int)Math.Clamp(Math.Round(maxSkor + bonus), 0, 100);
        }
    }
}
