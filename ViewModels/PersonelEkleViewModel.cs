using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class PersonelEkleViewModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Ad")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Soyad")]
        public string Soyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sicil numarası zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Sicil Numarası")]
        public string SicilNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birim alanı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Birim")]
        public string Birim { get; set; } = string.Empty;

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
    }
}