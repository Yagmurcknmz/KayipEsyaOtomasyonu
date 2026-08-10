using KayipEsyaOtomasyonu.Models;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class DashboardViewModel
    {
        public int ToplamKayipEsya { get; set; }

        public int DepodaBekleyen { get; set; }

        public int TeslimEdilen { get; set; }

        public int AktifKayipEsya { get; set; }

        public int ToplamVatandasBildirimi { get; set; }

        public int AktifBasvuru { get; set; }

        public int ToplamKullanici { get; set; }
        public int ToplamPersonel { get; set; }
        public int ToplamVatandas { get; set; }
        public int BekleyenEslesme { get; set; }

        public List<KayipEsya> SonKayipEsyalar { get; set; }
            = new List<KayipEsya>();

        public List<Models.KayipBildirimi> SonBasvurular { get; set; }
            = new List<Models.KayipBildirimi>();

        public List<Models.Eslesme> SonEslesmeler { get; set; }
            = new List<Models.Eslesme>();

        public List<DashboardKategoriGrafik> KategoriBazliDagilim { get; set; }
            = new List<DashboardKategoriGrafik>();

        public List<DashboardDurumGrafik> DurumBazliDagilim { get; set; }
            = new List<DashboardDurumGrafik>();
    }

    public class DashboardKategoriGrafik
    {
        public string KategoriAdi { get; set; } = string.Empty;
        public int Adet { get; set; }
        public double Yuzde { get; set; }
    }

    public class DashboardDurumGrafik
    {
        public string Durum { get; set; } = string.Empty;
        public int Adet { get; set; }
        public string Renk { get; set; } = "bg-primary";
    }
}



