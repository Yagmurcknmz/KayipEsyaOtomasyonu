using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public class KayipBildirimi
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Başvuru Numarası")]
        public string BasvuruNo { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string VatandasId { get; set; } = string.Empty;

        [ForeignKey(nameof(VatandasId))]
        public ApplicationUser? Vatandas { get; set; }

        [Required(ErrorMessage = "Eşya adı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Eşya Adı")]
        public string EsyaAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int KategoriId { get; set; }

        [ForeignKey(nameof(KategoriId))]
        public Kategori? Kategori { get; set; }

        [StringLength(100)]
        public string? Marka { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(50)]
        public string? Renk { get; set; }

        [Required(ErrorMessage = "Kayıp tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Kayıp Tarihi")]
        public DateTime KayipTarihi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Kayıp yeri zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Kayıp Yeri")]
        public string KayipYeri { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ayırt Edici Özellik")]
        public string? AyirtEdiciOzellik { get; set; }

        [StringLength(1000)]
        public string? Aciklama { get; set; }

        [Required]
        [StringLength(50)]
        public string Durum { get; set; } = "Başvuru Alındı";

        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;

        public DateTime? GuncellenmeTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

        [Display(Name = "Admin Notları")]
        public string? AdminNotu { get; set; }

        [Display(Name = "Kayıp Yeri (Enlem)")]
        public double? Enlem { get; set; }

        [Display(Name = "Kayıp Yeri (Boylam)")]
        public double? Boylam { get; set; }

        [StringLength(500)]
        [Display(Name = "Kayıp Yeri (Tam Adres)")]
        public string? AdresDetayi { get; set; }

        public List<KayipBildirimiResim> Resimler { get; set; } = new();
    }
}