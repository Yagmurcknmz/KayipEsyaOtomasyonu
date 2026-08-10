using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class ProfilDuzenleViewModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string Soyad { get; set; } = string.Empty;

        [StringLength(11, MinimumLength = 11, ErrorMessage = "TC Kimlik No 11 haneli olmalıdır.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "TC Kimlik No sadece rakamlardan oluşmalıdır.")]
        [Display(Name = "TC Kimlik Numarası")]
        public string? TcKimlikNo { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20)]
        [Display(Name = "Telefon Numarası")]
        public string? Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-Posta Adresi")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "İlçe / Mahalle")]
        public string? IlceMahalle { get; set; }

        [StringLength(250)]
        [Display(Name = "Açık Adres")]
        public string? Adres { get; set; }

        [StringLength(20)]
        [Display(Name = "Sicil No")]
        public string? SicilNo { get; set; }

        [StringLength(100)]
        [Display(Name = "Birim")]
        public string? Birim { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [Display(Name = "Yeni Şifre")]
        public string? YeniSifre { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler uyuşmuyor.")]
        [Display(Name = "Yeni Şifre Tekrar")]
        public string? YeniSifreTekrar { get; set; }
    }
}
