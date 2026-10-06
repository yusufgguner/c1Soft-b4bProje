using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace c1Soft_b4bProje.Models;

public class SiparisR
{
    [Key]
    public int SiparisId { get; set; }

    [Required]
    [StringLength(30)]
    public string SiparisNo { get; set; } = "";

    public int FirmaId { get; set; }

    public int KullaniciId { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public DateTime? GuncellemeTarihi { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BrutTutar { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VergiTutar { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GenelTutar { get; set; }

    [StringLength(500)]
    public string? Notu { get; set; }

    [StringLength(300)]
    public string? TeslimatAdresi { get; set; }

    [Required]
    [StringLength(30)]
    public string SiparisDurumu { get; set; } = "Pending";

    public Kullanici? Kullanici { get; set; }

    public List<SiparisD> Kalemler { get; set; } = new List<SiparisD>();
}
