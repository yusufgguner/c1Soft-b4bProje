using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Models;

public class KullaniciOturum
{
    [Key]
    public int OturumId { get; set; }

    public int KullaniciId { get; set; }

    public int FirmaId { get; set; }

    public Guid OturumAnahtari { get; set; } = Guid.NewGuid();

    [StringLength(200)]
    public string? Domain { get; set; }

    [StringLength(50)]
    public string? IpAdresi { get; set; }

    [StringLength(300)]
    public string? Tarayici { get; set; }

    public DateTime GirisTarihi { get; set; } = DateTime.Now;

    public DateTime SonIslemTarihi { get; set; } = DateTime.Now;

    public DateTime? CikisTarihi { get; set; }

    public bool IsAktif { get; set; } = true;

    [StringLength(300)]
    public string? KapanmaNedeni { get; set; }

    public Kullanici? Kullanici { get; set; }

    public Firma? Firma { get; set; }
}
