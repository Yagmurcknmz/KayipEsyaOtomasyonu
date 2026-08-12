using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayipEsyaOtomasyonu.Models
{
    public class KayipEsya
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Eşya adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Eşya adı en fazla 150 karakter olabilir.")]
        [Display(Name = "Eşya Adı")]
        public string EsyaAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int KategoriId { get; set; }

        [ForeignKey(nameof(KategoriId))]
        public Kategori? Kategori { get; set; }

        [StringLength(100)]
        [Display(Name = "Marka")]
        public string? Marka { get; set; }

        [StringLength(100)]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [StringLength(50)]
        [Display(Name = "Renk")]
        public string? Renk { get; set; }

        [StringLength(100)]
        [Display(Name = "Seri Numarası")]
        public string? SeriNo { get; set; }

        [StringLength(500)]
        [Display(Name = "Ayırt Edici Özellik")]
        public string? AyirtEdiciOzellik { get; set; }

        [Required(ErrorMessage = "Bulunma tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Bulunma Tarihi")]
        public DateTime BulunmaTarihi { get; set; } = DateTime.Today;

        [StringLength(200)]
        [Display(Name = "Bulunma Yeri")]
        public string? BulunmaYeri { get; set; }

        [StringLength(100)]
        [Display(Name = "Mahalle")]
        public string? Mahalle { get; set; }

        [StringLength(100)]
        [Display(Name = "Birim")]
        public string? Birim { get; set; }

        [StringLength(50)]
        [Display(Name = "Raf Numarası")]
        public string? RafNo { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Durum")]
        public string Durum { get; set; } = "Yeni Kayıt";

        [StringLength(1000)]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        public DateTime? GuncellenmeTarihi { get; set; }

        [Display(Name = "Bulunma Yeri (Enlem)")]
        public double? Enlem { get; set; }

        [Display(Name = "Bulunma Yeri (Boylam)")]
        public double? Boylam { get; set; }

        [StringLength(500)]
        [Display(Name = "Bulunma Yeri (Tam Adres)")]
        public string? AdresDetayi { get; set; }

        public List<KayipEsyaResim> Resimler { get; set; } = new();
    }
}