using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace KayipEsyaOtomasyonu.Services
{
    public class ResimYuklemeServisi : IResimYuklemeServisi
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string> IzinVerilenUzantilar = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };

        private const string UploadsAnaKlasor = "uploads";
        private const string ThumbnailsKlasorAdi = "thumbs";

        public ResimYuklemeServisi(
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResimYuklemeSonuc> TekliYukleAsync(
            IFormFile dosya,
            string? altKlasor = "genel",
            int thumbnailBoyutPx = 300,
            long maxBoyutBayt = 20 * 1024 * 1024,
            string? aciklama = null,
            string? yukleyenKullaniciId = null)
        {
            try
            {
                if (dosya == null || dosya.Length == 0)
                    return new ResimYuklemeSonuc(false, null, null, "Dosya boş.");

                if (dosya.Length > maxBoyutBayt)
                    return new ResimYuklemeSonuc(false, null, null, $"Dosya boyutu çok büyük (max: {maxBoyutBayt / 1024 / 1024} MB).");

                var uzanti = Path.GetExtension(dosya.FileName);
                if (string.IsNullOrWhiteSpace(uzanti) || !IzinVerilenUzantilar.Contains(uzanti))
                    return new ResimYuklemeSonuc(false, null, null, "İzin verilen resim uzantıları: .jpg, .jpeg, .png, .gif, .webp, .bmp");

                var yil = DateTime.Now.Year.ToString("D4");
                var ay = DateTime.Now.Month.ToString("D2");
                var gun = DateTime.Now.Day.ToString("D2");
                altKlasor = string.IsNullOrWhiteSpace(altKlasor) ? "genel" : altKlasor.Trim().Trim('/').Trim('\\');

                var goreliKlasor = $"/{UploadsAnaKlasor}/{altKlasor}/{yil}/{ay}/{gun}";
                var thumbnailGoreliKlasor = $"/{UploadsAnaKlasor}/{altKlasor}/{yil}/{ay}/{gun}/{ThumbnailsKlasorAdi}";

                var kayitKlasoru = Path.Combine(_env.WebRootPath, UploadsAnaKlasor, altKlasor, yil, ay, gun);
                var thumbnailKlasoru = Path.Combine(kayitKlasoru, ThumbnailsKlasorAdi);

                if (!Directory.Exists(kayitKlasoru)) Directory.CreateDirectory(kayitKlasoru);
                if (!Directory.Exists(thumbnailKlasoru)) Directory.CreateDirectory(thumbnailKlasoru);

                var benzersizAd = $"{Guid.NewGuid():N}_{DateTime.Now.Ticks}{uzanti.ToLowerInvariant()}";
                var tamOrjinalYol = Path.Combine(kayitKlasoru, benzersizAd);
                var tamThumbYol = Path.Combine(thumbnailKlasoru, benzersizAd);

                var goreliOrjinal = $"{goreliKlasor}/{benzersizAd}";
                var goreliThumb = $"{thumbnailGoreliKlasor}/{benzersizAd}";

                using (var stream = new FileStream(tamOrjinalYol, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await dosya.CopyToAsync(stream);
                }

                try
                {
                    using var img = await Image.LoadAsync(tamOrjinalYol);
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(thumbnailBoyutPx, thumbnailBoyutPx),
                        Mode = ResizeMode.Crop
                    }));
                    await img.SaveAsync(tamThumbYol);
                }
                catch
                {
                    // Thumbnail oluşturulamazsa orjinali thumb olarak kullan
                    goreliThumb = goreliOrjinal;
                }

                return new ResimYuklemeSonuc(true, goreliOrjinal, goreliThumb, null);
            }
            catch (Exception ex)
            {
                return new ResimYuklemeSonuc(false, null, null, $"Resim yüklenirken hata: {ex.Message}");
            }
        }

        public async Task<List<ResimYuklemeSonuc>> CokluYukleAsync(
            IEnumerable<IFormFile> dosyalar,
            string? altKlasor = "genel",
            int thumbnailBoyutPx = 300,
            long maxDosyaBoyutu = 20 * 1024 * 1024,
            string? yukleyenKullaniciId = null)
        {
            var sonuclar = new List<ResimYuklemeSonuc>();
            if (dosyalar == null) return sonuclar;

            foreach (var dosya in dosyalar.Where(d => d != null && d.Length > 0))
            {
                var s = await TekliYukleAsync(
                    dosya,
                    altKlasor,
                    thumbnailBoyutPx,
                    maxDosyaBoyutu,
                    null,
                    yukleyenKullaniciId);
                sonuclar.Add(s);
            }
            return sonuclar;
        }

        public async Task<bool> DosyaSilAsync(string? goreliDizin)
        {
            if (string.IsNullOrWhiteSpace(goreliDizin)) return false;
            try
            {
                var temiz = goreliDizin.Trim().Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                var tamYol = Path.Combine(_env.WebRootPath, temiz);
                if (File.Exists(tamYol))
                {
                    File.Delete(tamYol);
                    return true;
                }
                await Task.CompletedTask;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public string ResimTamUrl(string goreliDizin)
        {
            if (string.IsNullOrWhiteSpace(goreliDizin)) return string.Empty;
            if (goreliDizin.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return goreliDizin;
            if (!goreliDizin.StartsWith("/")) goreliDizin = "/" + goreliDizin;
            var req = _httpContextAccessor?.HttpContext?.Request;
            if (req == null) return goreliDizin;
            return $"{req.Scheme}://{req.Host}{goreliDizin}";
        }
    }
}
