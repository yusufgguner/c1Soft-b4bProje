using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Dtos;

public class KullaniciDto
{
    public int KullaniciId { get; set; }
    public int FirmaId { get; set; }
    public string KulAdi { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string? Email { get; set; }
    public string Rol { get; set; } = "";
    public bool IsAktif { get; set; }
    public DateTime? SonGirisTarihi { get; set; }
}

public class KullaniciEkleRequest
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50)]
    public string KulAdi { get; set; } = "";

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string Sifre { get; set; } = "";

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    public string AdSoyad { get; set; } = "";

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [Required]
    public string Rol { get; set; } = "Kullanici";
}

public class KullaniciGuncelleRequest
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    public string AdSoyad { get; set; } = "";

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [Required]
    public string Rol { get; set; } = "Kullanici";

    public bool IsAktif { get; set; } = true;

    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string? YeniSifre { get; set; }
}
