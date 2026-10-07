using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Models;

public class IslemLog
{
    [Key]
    public long LogId { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public int? KullaniciId { get; set; }

    public int? FirmaId { get; set; }

    [StringLength(50)]
    public string? KulAdi { get; set; }

    [Required]
    [StringLength(30)]
    public string Tur { get; set; } = LogTurleri.Istek;

    [StringLength(10)]
    public string? Metot { get; set; }

    [StringLength(300)]
    public string? Yol { get; set; }

    public int? DurumKodu { get; set; }

    [StringLength(200)]
    public string? Domain { get; set; }

    [StringLength(50)]
    public string? IpAdresi { get; set; }

    public int? SureMs { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}
