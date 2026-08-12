using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public class KayipEsyaResim
    {
        public int Id { get; set; }

        [Required]
        public int KayipEsyaId { get; set; }

        [ForeignKey(nameof(KayipEsyaId))]
        public KayipEsya? KayipEsya { get; set; }

        [Required(ErrorMessage = "Resim dosya yolu zorunludur.")]
        [StringLength(500)]
        [Display(Name = "Orijinal Resim")]
        public string DosyaYolu { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Küçük Resim (Thumbnail)")]
        public string? ThumbnailYolu { get; set; }

        [StringLength(200)]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Display(Name = "Sıra")]
        public int SiraNumarasi { get; set; } = 0;

        [Display(Name = "Varsayılan")]
        public bool VarsayilanResimMi { get; set; } = false;

        public bool AktifMi { get; set; } = true;

        [StringLength(50)]
        public string? YukleyenKullaniciId { get; set; }

        public DateTime YuklenmeTarihi { get; set; } = DateTime.Now;
    }
}
