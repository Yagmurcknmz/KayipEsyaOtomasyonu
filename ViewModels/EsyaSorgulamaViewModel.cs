using KayipEsyaOtomasyonu.Models;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class EsyaSorgulamaViewModel
    {
        public string? Aranan { get; set; }
        public int? KategoriId { get; set; }

        public List<KayipEsya> BulunanEsyalar { get; set; } = new();
        public List<KayipBildirimi> KendiKayipBildirilerim { get; set; } = new();
    }
}
