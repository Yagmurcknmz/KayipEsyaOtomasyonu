using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public enum EslesmeDurumu
    {
        [Display(Name = "Beklemede")]
        Beklemede = 0,
        [Display(Name = "Onaylandı")]
        Onaylandi = 1,
        [Display(Name = "Reddedildi")]
        Reddedildi = 2,
        [Display(Name = "Teslim Edildi")]
        TeslimEdildi = 3
    }

    public enum EslesmeTuru
    {
        [Display(Name = "Otomatik")]
        Otomatik = 0,
        [Display(Name = "Manuel")]
        Manuel = 1
    }

    public class Eslesme
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kayıp başvurusu zorunludur.")]
        [Display(Name = "Kayıp Başvurusu")]
        public int KayipBildirimiId { get; set; }

        [ForeignKey(nameof(KayipBildirimiId))]
        public KayipBildirimi? KayipBildirimi { get; set; }

        [Required(ErrorMessage = "Bulunan eşya zorunludur.")]
        [Display(Name = "Bulunan Eşya")]
        public int KayipEsyaId { get; set; }

        [ForeignKey(nameof(KayipEsyaId))]
        public KayipEsya? KayipEsya { get; set; }

        [Required]
        [Display(Name = "Eşleşme Türü")]
        public EslesmeTuru Tur { get; set; } = EslesmeTuru.Otomatik;

        [Required]
        [Display(Name = "Eşleşme Durumu")]
        public EslesmeDurumu Durum { get; set; } = EslesmeDurumu.Beklemede;

        [Display(Name = "Eşleşme Skoru")]
        [Range(0, 100, ErrorMessage = "Skor 0-100 arasında olmalıdır.")]
        public int Skor { get; set; }

        [Display(Name = "Eşleşme Detayı")]
        [StringLength(500)]
        public string? EslesmeDetay { get; set; }

        [Display(Name = "Admin Notu")]
        [StringLength(1000)]
        public string? AdminNotu { get; set; }

        [Display(Name = "Onaylayan Admin")]
        [StringLength(100)]
        public string? OnaylayanAdmin { get; set; }

        [Display(Name = "Onay/Reddetme Tarihi")]
        public DateTime? IslemTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
