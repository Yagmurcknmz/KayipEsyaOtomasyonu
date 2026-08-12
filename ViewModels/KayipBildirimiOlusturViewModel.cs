using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KayipEsyaOtomasyonu.ViewModels
{
    public class KayipBildirimiOlusturViewModel
    {
        [Required(ErrorMessage = "Eşya adı zorunludur.")]
        [StringLength(
            150,
            ErrorMessage = "Eşya adı en fazla 150 karakter olabilir.")]
        [Display(Name = "Eşya Adı")]
        public string EsyaAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int? KategoriId { get; set; }

        [StringLength(100)]
        [Display(Name = "Marka")]
        public string? Marka { get; set; }

        [StringLength(100)]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [StringLength(50)]
        [Display(Name = "Renk")]
        public string? Renk { get; set; }

        [Required(ErrorMessage = "Kayıp tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Kayıp Tarihi")]
        public DateTime KayipTarihi { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Kayıp yeri zorunludur.")]
        [StringLength(
            200,
            ErrorMessage = "Kayıp yeri en fazla 200 karakter olabilir.")]
        [Display(Name = "Kayıp Yeri")]
        public string KayipYeri { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ayırt Edici Özellik")]
        public string? AyirtEdiciOzellik { get; set; }

        [StringLength(1000)]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Display(Name = "Kayıp Yeri (Enlem - Haritadan seçilecek)")]
        public double? Enlem { get; set; }

        [Display(Name = "Kayıp Yeri (Boylam - Haritadan seçilecek)")]
        public double? Boylam { get; set; }

        [StringLength(500)]
        [Display(Name = "Kayıp Yeri (Tam Adres / Not)")]
        public string? AdresDetayi { get; set; }

        [Display(Name = "Eşya Fotoğrafları (En fazla 5 adet)")]
        public IEnumerable<IFormFile>? ResimDosyalari { get; set; }
    }
}