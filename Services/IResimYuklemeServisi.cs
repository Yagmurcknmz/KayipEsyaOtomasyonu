using KayipEsyaOtomasyonu.Models;
using Microsoft.AspNetCore.Http;

namespace KayipEsyaOtomasyonu.Services
{
    public record ResimYuklemeSonuc(
        bool Basarili,
        string? DosyaYolu,
        string? ThumbnailYolu,
        string? HataMesaji);

    public interface IResimYuklemeServisi
    {
        Task<ResimYuklemeSonuc> TekliYukleAsync(
            IFormFile dosya,
            string? altKlasor = "genel",
            int thumbnailBoyutPx = 300,
            long maxBoyutBayt = 20 * 1024 * 1024,
            string? aciklama = null,
            string? yukleyenKullaniciId = null);

        Task<List<ResimYuklemeSonuc>> CokluYukleAsync(
            IEnumerable<IFormFile> dosyalar,
            string? altKlasor = "genel",
            int thumbnailBoyutPx = 300,
            long maxDosyaBoyutu = 20 * 1024 * 1024,
            string? yukleyenKullaniciId = null);

        Task<bool> DosyaSilAsync(string? goreliDizin);

        string ResimTamUrl(string goreliDizin);
    }
}
