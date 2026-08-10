namespace KayipEsyaOtomasyonu.ViewModels
{
    public class KullaniciViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string AdSoyad { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Telefon { get; set; }

        public string? TcKimlikNo { get; set; }

        public string? IlceMahalle { get; set; }

        public string? Adres { get; set; }

        public string Rol { get; set; } = string.Empty;

        public string? Birim { get; set; }

        public string? SicilNo { get; set; }

        public bool AktifMi { get; set; }

        public DateTime KayitTarihi { get; set; }
    }
}