using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class EmailDegistirViewModel
    {
        [Required(ErrorMessage = "Mevcut şifrenizi giriniz.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre *")]
        public string MevcutSifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "Yeni E-posta Adresi *")]
        public string YeniEmail { get; set; } = string.Empty;

        [DataType(DataType.EmailAddress)]
        [Display(Name = "Yeni E-posta (Tekrar) *")]
        [Compare("YeniEmail", ErrorMessage = "E-posta adresleri birbiriyle uyuşmuyor.")]
        public string YeniEmailTekrar { get; set; } = string.Empty;
    }
}
