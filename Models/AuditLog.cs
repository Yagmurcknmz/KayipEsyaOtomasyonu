using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public enum AuditTip
    {
        [Display(Name = "Oluşturuldu")]
        Create = 1,
        [Display(Name = "Güncellendi")]
        Update = 2,
        [Display(Name = "Silindi")]
        Delete = 3,
        [Display(Name = "Giriş")]
        Login = 4,
        [Display(Name = "Çıkış")]
        Logout = 5,
        [Display(Name = "Özel")]
        Custom = 99
    }

    public class AuditLog
    {
        public long Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [StringLength(200)]
        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }

        [Required]
        [Display(Name = "İşlem Tipi")]
        public AuditTip Tip { get; set; } = AuditTip.Custom;

        [StringLength(100)]
        [Display(Name = "Tablo")]
        public string? TabloAdi { get; set; }

        [Display(Name = "Kayıt Id")]
        public long? KayitId { get; set; }

        [StringLength(100)]
        [Display(Name = "Kayıt Anahtarı")]
        public string? KayitAnahtari { get; set; }

        [Display(Name = "Eski Değerler")]
        public string? EskiDegerlerJson { get; set; }

        [Display(Name = "Yeni Değerler")]
        public string? YeniDegerlerJson { get; set; }

        [StringLength(2000)]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [StringLength(100)]
        [Display(Name = "IP Adresi")]
        public string? IpAdresi { get; set; }

        [StringLength(1000)]
        [Display(Name = "Cihaz Tarayıcı")]
        public string? UserAgent { get; set; }

        [Display(Name = "Tarih")]
        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
