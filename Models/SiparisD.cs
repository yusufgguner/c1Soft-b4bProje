using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace c1Soft_b4bProje.Models;

public class SiparisD
{
    [Key]
    public int Sayac { get; set; }

    public int SiparisId { get; set; }

    public int UrunId { get; set; }

    [Required]
    [StringLength(50)]
    public string UrunKodu { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string UrunAdi { get; set; } = "";

    public int Miktar { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimFiyat { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BirimTutar { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal KDVOrani { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal KDVTutari { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GenelToplam { get; set; }

    public SiparisR? Siparis { get; set; }

    public Product? Urun { get; set; }
}
