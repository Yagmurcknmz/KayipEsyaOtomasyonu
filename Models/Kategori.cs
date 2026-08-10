using System.ComponentModel.DataAnnotations;

namespace KayipEsyaOtomasyonu.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
        public string Ad { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir.")]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}