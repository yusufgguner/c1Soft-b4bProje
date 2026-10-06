using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Models;

public class HataLog
{
    [Key]
    public int HataId { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public int? KullaniciId { get; set; }

    public int? FirmaId { get; set; }

    [StringLength(50)]
    public string? KulAdi { get; set; }

    [StringLength(10)]
    public string? Metot { get; set; }

    [StringLength(300)]
    public string? Yol { get; set; }

    [StringLength(200)]
    public string? Domain { get; set; }

    [StringLength(50)]
    public string? IpAdresi { get; set; }

    [Required]
    [StringLength(200)]
    public string HataTipi { get; set; } = "";

    [Required]
    [StringLength(1000)]
    public string Mesaj { get; set; } = "";

    public string? Detay { get; set; }

    public bool IsCozuldu { get; set; }

    public DateTime? CozulmeTarihi { get; set; }
}
