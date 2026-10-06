using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Models;

public class Kullanici
{
    [Key]
    public int KullaniciId { get; set; }

    public int FirmaId { get; set; }

    [Required]
    [StringLength(50)]
    public string KulAdi { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string SifreHash { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = "";

    [StringLength(150)]
    public string? Email { get; set; }

    [Required]
    [StringLength(20)]
    public string Rol { get; set; } = Roller.Kullanici;

    public bool IsAktif { get; set; } = true;

    public DateTime? SonGirisTarihi { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public Firma? Firma { get; set; }
}
