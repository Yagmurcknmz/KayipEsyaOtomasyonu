namespace KayipEsyaOtomasyonu.ViewModels
{
    public class ConfirmEmailViewModel
    {
        public bool BasariliMi { get; set; }
        public string? Mesaj { get; set; }
        public string? HataDetayi { get; set; }
        public string? DonusLinki { get; set; } = "/Account/Login";
    }
}
