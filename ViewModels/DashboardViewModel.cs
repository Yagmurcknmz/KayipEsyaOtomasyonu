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

        public int OnaylananEslesme { get; set; }
        public int ReddedilenEslesme { get; set; }
        public int BugunYeniKayit { get; set; }
        public int BugunYeniBasvuru { get; set; }
        public double TeslimOraniYuzde { get; set; }

        public List<KayipEsya> SonKayipEsyalar { get; set; } = new();
        public List<KayipBildirimi> SonBasvurular { get; set; } = new();
        public List<Eslesme> SonEslesmeler { get; set; } = new();

        public List<DashboardKategoriGrafik> KategoriBazliDagilim { get; set; } = new();
        public List<DashboardDurumGrafik> DurumBazliDagilim { get; set; } = new();

        public List<GunlukVeriNoktasi> Son30GunVeri { get; set; } = new();
        public List<MahalleEnvanterGrafik> MahalleTop5 { get; set; } = new();

        public List<AylikOzet> AylikOzet { get; set; } = new();
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

    public class GunlukVeriNoktasi
    {
        public DateTime Tarih { get; set; }
        public int BulunanEsya { get; set; }
        public int KayipBasvuru { get; set; }
        public int Eslesti { get; set; }
        public int Teslim { get; set; }
    }

    public class MahalleEnvanterGrafik
    {
        public string Mahalle { get; set; } = string.Empty;
        public int BulunanEsya { get; set; }
        public int KayipBasvuru { get; set; }
        public int Toplam => BulunanEsya + KayipBasvuru;
    }

    public class AylikOzet
    {
        public int Ay { get; set; }
        public string AyAdi { get; set; } = string.Empty;
        public int Bulunan { get; set; }
        public int Kayip { get; set; }
        public int Teslim { get; set; }
    }

    public class AuditLogIndexViewModel
    {
        public int Sayfa { get; set; } = 1;
        public int SayfaBoyutu { get; set; } = 25;
        public int ToplamKayit { get; set; }
        public string? Ara { get; set; }
        public string? TabloAdi { get; set; }
        public AuditTip? Tip { get; set; }
        public string? UserId { get; set; }
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public List<AuditLog> Kayitlar { get; set; } = new();
        public List<string> TabloAdlari { get; set; } = new();
        public int ToplamSayfa => (int)Math.Ceiling(ToplamKayit * 1.0 / Math.Max(1, SayfaBoyutu));
    }
}
