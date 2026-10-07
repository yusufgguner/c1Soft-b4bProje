using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "Firma kodu zorunludur.")]
    public string FirmaKodu { get; set; } = "";

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string KulAdi { get; set; } = "";

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Sifre { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime BitisTarihi { get; set; }
    public int KullaniciId { get; set; }
    public string KulAdi { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string Rol { get; set; } = "";
    public int FirmaId { get; set; }
    public string FirmaKodu { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
}
