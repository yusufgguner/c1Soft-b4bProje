namespace c1Soft_b4bProje.Areas.Admin.Models;

public class PanelOzet
{
    public int FirmaSayisi { get; set; }
    public int KullaniciSayisi { get; set; }
    public int BugunGiris { get; set; }
    public int AktifOturum { get; set; }
    public int BugunDusurulenOturum { get; set; }
    public int BugunSiparis { get; set; }
    public decimal BugunCiro { get; set; }
    public int AcikHata { get; set; }
    public int BugunHata { get; set; }
    public List<SiparisSatiri> SonSiparisler { get; set; } = new List<SiparisSatiri>();
    public List<OturumSatiri> SonGirisler { get; set; } = new List<OturumSatiri>();
}

public class SiparisSatiri
{
    public int SiparisId { get; set; }
    public string SiparisNo { get; set; } = "";
    public int FirmaId { get; set; }
    public string FirmaKodu { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
    public string KulAdi { get; set; } = "";
    public DateTime Tarih { get; set; }
    public decimal GenelTutar { get; set; }
    public string SiparisDurumu { get; set; } = "";
    public int KalemSayisi { get; set; }
}

public class OturumSatiri
{
    public int OturumId { get; set; }
    public string KulAdi { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string FirmaKodu { get; set; } = "";
    public string? Domain { get; set; }
    public string? IpAdresi { get; set; }
    public string? Tarayici { get; set; }
    public DateTime GirisTarihi { get; set; }
    public DateTime SonIslemTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    public bool IsAktif { get; set; }
    public string? KapanmaNedeni { get; set; }

    public bool DomainDegisimi => KapanmaNedeni != null && KapanmaNedeni.StartsWith("Farklı domainden giriş");
}

public class KullaniciSatiri
{
    public int KullaniciId { get; set; }
    public string KulAdi { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public string? Email { get; set; }
    public string Rol { get; set; } = "";
    public string FirmaKodu { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
    public bool IsAktif { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? SonGirisTarihi { get; set; }
}

public class FiltreSecenegi
{
    public int? FirmaId { get; set; }
    public string? Durum { get; set; }
    public string? Tur { get; set; }
    public string? Arama { get; set; }
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }
}
