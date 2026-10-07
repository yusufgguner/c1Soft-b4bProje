using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Dtos;

public class FirmaDto
{
    public int FirmaId { get; set; }
    public string FirmaKodu { get; set; } = "";
    public string Adi { get; set; } = "";
    public string? VergiNo { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    public string? Sehir { get; set; }
    public bool IsAktif { get; set; }
    public int KullaniciSayisi { get; set; }
}

public class FirmaKaydetRequest
{
    [Required(ErrorMessage = "Firma kodu zorunludur.")]
    [StringLength(20)]
    public string FirmaKodu { get; set; } = "";

    [Required(ErrorMessage = "Firma adı zorunludur.")]
    [StringLength(150)]
    public string Adi { get; set; } = "";

    [StringLength(20)]
    public string? VergiNo { get; set; }

    [StringLength(30)]
    public string? Telefon { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Sehir { get; set; }

    public bool IsAktif { get; set; } = true;
}
