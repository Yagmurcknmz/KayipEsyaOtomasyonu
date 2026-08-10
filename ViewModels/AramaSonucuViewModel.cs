namespace KayipEsyaOtomasyonu.ViewModels
{
    public class AramaSonucuViewModel
    {
        public string? AramaKelimesi { get; set; }

        public int ToplamEsya { get; set; }
        public int ToplamBasvuru { get; set; }
        public int ToplamEslesme { get; set; }
        public int ToplamKullanici { get; set; }

        public List<Models.KayipEsya> Esyalar { get; set; } = new();
        public List<Models.KayipBildirimi> Basvurular { get; set; } = new();
        public List<Models.Eslesme> Eslesmeler { get; set; } = new();
        public List<Models.ApplicationUser> Kullanicilar { get; set; } = new();
    }
}
