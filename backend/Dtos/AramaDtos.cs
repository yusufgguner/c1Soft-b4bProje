namespace c1Soft_b4bProje.Dtos;

public class UrunAramaIstegi
{
    public string? Q { get; set; }
    public string? Marka { get; set; }
    public decimal? MinFiyat { get; set; }
    public decimal? MaxFiyat { get; set; }
    public bool StoktaOlan { get; set; }
    public bool SadeceAktif { get; set; } = true;
    public int Sayfa { get; set; } = 1;
    public int Adet { get; set; } = 20;
}

public class UrunAramaSonucu
{
    public long Toplam { get; set; }
    public int Sayfa { get; set; }
    public List<ProductDto> Urunler { get; set; } = new();
}
