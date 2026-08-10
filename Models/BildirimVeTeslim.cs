using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public enum BildirimTuru
    {
        [Display(Name = "Eşleşme Bulundu")]
        EslesmeBulundu = 0,
        [Display(Name = "Teslim Onayı")]
        TeslimOnayi = 1,
        [Display(Name = "Başvuru Durumu")]
        BasvuruDurumu = 2,
        [Display(Name = "Genel Duyuru")]
        GenelDuyuru = 3
    }

    public class Bildirim
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Alıcı Kullanıcı")]
        [StringLength(450)]
        public string AliciUserId { get; set; } = "";

        [ForeignKey(nameof(AliciUserId))]
        public ApplicationUser? Alici { get; set; }

        [Display(Name = "Başvuru Numarası")]
        public int? KayipBildirimiId { get; set; }

        [ForeignKey(nameof(KayipBildirimiId))]
        public KayipBildirimi? KayipBildirimi { get; set; }

        [Display(Name = "Eşleşme Numarası")]
        public int? EslesmeId { get; set; }

        [ForeignKey(nameof(EslesmeId))]
        public Eslesme? Eslesme { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Bildirim Başlığı")]
        public string Baslik { get; set; } = "";

        [StringLength(1000)]
        [Display(Name = "Bildirim İçeriği")]
        public string? Icerik { get; set; }

        [Required]
        [Display(Name = "Bildirim Türü")]
        public BildirimTuru Turu { get; set; } = BildirimTuru.BasvuruDurumu;

        [Display(Name = "Okundu")]
        public bool OkunduMu { get; set; } = false;

        [Display(Name = "Okunma Tarihi")]
        public DateTime? OkunmaTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }

    public class TeslimIslemi
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "İlgili Eşleşme")]
        public int EslesmeId { get; set; }

        [ForeignKey(nameof(EslesmeId))]
        public Eslesme? Eslesme { get; set; }

        [Display(Name = "Teslim Eden Personel")]
        [StringLength(450)]
        public string? TeslimEdenUserId { get; set; }

        [ForeignKey(nameof(TeslimEdenUserId))]
        public ApplicationUser? TeslimEden { get; set; }

        [Display(Name = "Teslim Alan Kişi")]
        [StringLength(150)]
        public string? TeslimAlanKisi { get; set; }

        [Display(Name = "T.C. Kimlik No")]
        [StringLength(11)]
        public string? TcKimlikNo { get; set; }

        [Display(Name = "İletişim Telefonu")]
        [StringLength(20)]
        public string? IletisimTelefonu { get; set; }

        [Required(ErrorMessage = "Teslim tarihi zorunludur.")]
        [Display(Name = "Teslim Tarihi")]
        public DateTime TeslimTarihi { get; set; } = DateTime.Now;

        [Display(Name = "Teslim Saati")]
        public TimeSpan? TeslimSaati { get; set; } = null;

        [Display(Name = "Teslim Yeri")]
        [StringLength(250)]
        public string? TeslimYeri { get; set; } = "Arnavutköy Belediyesi";

        [Display(Name = "Teslim Şekli")]
        [StringLength(100)]
        public string? TeslimSekli { get; set; } = "Şahsen";

        [Display(Name = "İmza / Onay")]
        public bool ImzaOnayi { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Ek Notlar")]
        public string? EkNotlar { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
