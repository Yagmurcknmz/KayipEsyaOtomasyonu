using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class VatandasKayitViewModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(
            50,
            ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        [Display(Name = "Ad")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(
            50,
            ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        [Display(Name = "Soyad")]
        public string Soyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(
            ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [Phone(
            ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(
            20,
            ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string Telefon { get; set; } = string.Empty;

        [Required(ErrorMessage = "TC Kimlik No zorunludur.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "TC Kimlik No 11 haneli olmalıdır.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "TC Kimlik No sadece rakamlardan oluşmalıdır.")]
        [Display(Name = "TC Kimlik Numarası")]
        public string TcKimlikNo { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "İlçe / Mahalle")]
        public string? IlceMahalle { get; set; }

        [StringLength(250)]
        [Display(Name = "Açık Adres")]
        public string? Adres { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Sifre),
            ErrorMessage = "Şifreler birbiriyle uyuşmuyor.")]
        [Display(Name = "Şifre Tekrar")]
        public string SifreTekrar { get; set; } = string.Empty;

        [Display(
            Name = "Aydınlatma metnini okudum ve kabul ediyorum.")]
        public bool AydinlatmaMetniOnayi { get; set; }
    }
}