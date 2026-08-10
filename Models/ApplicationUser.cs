using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50)]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50)]
        public string Soyad { get; set; } = string.Empty;

        [StringLength(11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "TC Kimlik No 11 haneli olmalıdır.")]
        [Display(Name = "TC Kimlik No")]
        public string? TcKimlikNo { get; set; }

        [StringLength(250)]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        [StringLength(100)]
        [Display(Name = "İlçe / Mahalle")]
        public string? IlceMahalle { get; set; }

        [StringLength(20)]
        public string? SicilNo { get; set; }

        [StringLength(100)]
        public string? Birim { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}