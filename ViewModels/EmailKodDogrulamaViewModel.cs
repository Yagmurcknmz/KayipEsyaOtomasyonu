using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class EmailKodDogrulamaViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Purpose { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta Adresi")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(20, ErrorMessage = "Doğrulama kodunu kontrol ediniz.")]
        [Display(Name = "Doğrulama Kodu")]
        public string Code { get; set; } = string.Empty;
    }
}
