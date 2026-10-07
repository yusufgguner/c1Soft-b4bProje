using System.ComponentModel.DataAnnotations;

namespace c1Soft_b4bProje.Dtos;

public class SiparisDto
{
    public int SiparisId { get; set; }
    public string SiparisNo { get; set; } = "";
    public int KullaniciId { get; set; }
    public string KulAdi { get; set; } = "";
    public DateTime Tarih { get; set; }
    public decimal BrutTutar { get; set; }
    public decimal VergiTutar { get; set; }
    public decimal GenelTutar { get; set; }
    public string? Notu { get; set; }
    public string? TeslimatAdresi { get; set; }
    public string SiparisDurumu { get; set; } = "";
    public List<SiparisKalemDto> Kalemler { get; set; } = new List<SiparisKalemDto>();
}

public class SiparisKalemDto
{
    public int UrunId { get; set; }
    public string UrunKodu { get; set; } = "";
    public string UrunAdi { get; set; } = "";
    public int Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal BirimTutar { get; set; }
    public decimal KDVOrani { get; set; }
    public decimal KDVTutari { get; set; }
    public decimal GenelToplam { get; set; }
}

public class SiparisOlusturRequest
{
    [Required(ErrorMessage = "Teslimat adresi zorunludur.")]
    [StringLength(300)]
    public string TeslimatAdresi { get; set; } = "";

    [StringLength(500)]
    public string? Notu { get; set; }

    [MinLength(1, ErrorMessage = "Siparişte en az bir ürün olmalı.")]
    public List<SiparisKalemRequest> Kalemler { get; set; } = new List<SiparisKalemRequest>();
}

public class SiparisKalemRequest
{
    public int UrunId { get; set; }

    [Range(1, 100000, ErrorMessage = "Miktar en az 1 olmalı.")]
    public int Miktar { get; set; }
}

public class SiparisDurumRequest
{
    [Required]
    public string SiparisDurumu { get; set; } = "";
}
