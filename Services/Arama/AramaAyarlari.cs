namespace c1Soft_b4bProje.Services.Arama;

public class AramaAyarlari
{
    public string ElasticUrl { get; set; } = "http://localhost:9200";
    public string ElasticKullanici { get; set; } = "elastic";
    public string ElasticSifre { get; set; } = "";
    public string Indeks { get; set; } = "b4b-urunler";
    public string RedisBaglanti { get; set; } = "localhost:6379";
    public int BellekSaniye { get; set; } = 60;
    public int RedisDakika { get; set; } = 10;
}
