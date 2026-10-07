using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Models;

public class Firma
{
    [Key]
    public int FirmaId { get; set; }

    [Required]
    [StringLength(20)]
    public string FirmaKodu { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string Adi { get; set; } = "";

    [StringLength(20)]
    public string? VergiNo { get; set; }

    [StringLength(30)]
    public string? Telefon { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Sehir { get; set; }

    public bool IsAktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}
