using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool BeniHatirla { get; set; }
    }
}
