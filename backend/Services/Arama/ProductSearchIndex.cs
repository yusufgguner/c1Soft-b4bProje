using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using Microsoft.Extensions.Options;

namespace c1Soft_b4bProje.Services.Arama;

public class ProductSearchIndex
{
    private const string IndeksAyari = """
    {
      "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "analysis": {
          "filter": {
            "tr_kucuk": { "type": "lowercase", "language": "turkish" },
            "kod_onek": { "type": "edge_ngram", "min_gram": 1, "max_gram": 20 },
            "ad_onek": { "type": "edge_ngram", "min_gram": 2, "max_gram": 15 }
          },
          "normalizer": {
            "kod_norm": { "type": "custom", "filter": ["lowercase", "asciifolding"] }
          },
          "analyzer": {
            "tr_metin": { "type": "custom", "tokenizer": "standard", "filter": ["tr_kucuk", "asciifolding"] },
            "kod_onek": { "type": "custom", "tokenizer": "keyword", "filter": ["lowercase", "asciifolding", "kod_onek"] },
            "kod_ara": { "type": "custom", "tokenizer": "keyword", "filter": ["lowercase", "asciifolding"] },
            "ad_onek": { "type": "custom", "tokenizer": "standard", "filter": ["tr_kucuk", "asciifolding", "ad_onek"] }
          }
        }
      },
      "mappings": {
        "properties": {
          "productId": { "type": "integer" },
          "firmaId": { "type": "integer" },
          "productCode": {
            "type": "keyword",
            "normalizer": "kod_norm",
            "fields": { "onek": { "type": "text", "analyzer": "kod_onek", "search_analyzer": "kod_ara" } }
          },
          "productName": {
            "type": "text",
            "analyzer": "tr_metin",
            "fields": {
              "sirala": { "type": "keyword" },
              "onek": { "type": "text", "analyzer": "ad_onek", "search_analyzer": "tr_metin" }
            }
          },
          "brand": {
            "type": "text",
            "analyzer": "tr_metin",
            "fields": { "tam": { "type": "keyword", "normalizer": "kod_norm" } }
          },
          "price": { "type": "double" },
          "stockQuantity": { "type": "integer" },
          "isActive": { "type": "boolean" }
        }
      }
    }
    """;

    private readonly HttpClient http;
    private readonly string indeks;

    public ProductSearchIndex(IOptions<AramaAyarlari> ayarlar)
    {
        var a = ayarlar.Value;
        indeks = a.Indeks;
        http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
        {
            BaseAddress = new Uri(a.ElasticUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        string kimlik = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{a.ElasticKullanici}:{a.ElasticSifre}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", kimlik);
    }

    // İndeks var mı diye bakar
    public async Task<bool> VarMiAsync()
    {
        using var istek = new HttpRequestMessage(HttpMethod.Head, indeks);
        using var cevap = await http.SendAsync(istek);
        return cevap.StatusCode == HttpStatusCode.OK;
    }

    // İndeksi siler ve ayarlarıyla yeniden kurar
    public async Task YenidenKurAsync()
    {
        using (await http.DeleteAsync(indeks)) { }
        using var icerik = new StringContent(IndeksAyari, Encoding.UTF8, "application/json");
        using var cevap = await http.PutAsync(indeks, icerik);
        await HataVarsaFirlat(cevap);
    }

    // Ürünleri toplu ekler veya günceller, silinenleri indeksten çıkarır
    public async Task TopluYazAsync(IEnumerable<Product> yazilacak, IEnumerable<int> silinecek)
    {
        var sb = new StringBuilder();

        foreach (var urun in yazilacak)
        {
            sb.Append("{\"index\":{\"_id\":\"").Append(urun.ProductId).Append("\"}}\n");
            sb.Append(JsonSerializer.Serialize(new
            {
                productId = urun.ProductId,
                firmaId = urun.FirmaId,
                productCode = urun.ProductCode,
                productName = urun.ProductName,
                brand = urun.Brand,
                price = urun.Price,
                stockQuantity = urun.StockQuantity,
                isActive = urun.IsActive
            })).Append('\n');
        }

        foreach (int id in silinecek)
        {
            sb.Append("{\"delete\":{\"_id\":\"").Append(id).Append("\"}}\n");
        }

        if (sb.Length == 0)
        {
            return;
        }

        using var icerik = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
        using var cevap = await http.PostAsync($"{indeks}/_bulk?refresh=true", icerik);
        await HataVarsaFirlat(cevap);
    }

    // Firmanın ürünlerinde arama yapar
    public async Task<UrunAramaSonucu> AraAsync(int firmaId, UrunAramaIstegi istek)
    {
        var filtre = new JsonArray { new JsonObject { ["term"] = new JsonObject { ["firmaId"] = firmaId } } };

        if (istek.SadeceAktif)
        {
            filtre.Add(new JsonObject { ["term"] = new JsonObject { ["isActive"] = true } });
        }

        if (!string.IsNullOrWhiteSpace(istek.Marka))
        {
            filtre.Add(new JsonObject { ["term"] = new JsonObject { ["brand.tam"] = istek.Marka.Trim() } });
        }

        if (istek.MinFiyat != null || istek.MaxFiyat != null)
        {
            var aralik = new JsonObject();
            if (istek.MinFiyat != null) aralik["gte"] = istek.MinFiyat.Value;
            if (istek.MaxFiyat != null) aralik["lte"] = istek.MaxFiyat.Value;
            filtre.Add(new JsonObject { ["range"] = new JsonObject { ["price"] = aralik } });
        }

        if (istek.StoktaOlan)
        {
            filtre.Add(new JsonObject { ["range"] = new JsonObject { ["stockQuantity"] = new JsonObject { ["gt"] = 0 } } });
        }

        var kosul = new JsonObject { ["filter"] = filtre };
        var siralama = new JsonArray();
        string? q = istek.Q?.Trim();

        if (!string.IsNullOrEmpty(q))
        {
            kosul["should"] = new JsonArray
            {
                new JsonObject { ["term"] = new JsonObject { ["productCode"] = new JsonObject { ["value"] = q, ["boost"] = 10 } } },
                new JsonObject { ["match"] = new JsonObject { ["productCode.onek"] = new JsonObject { ["query"] = q, ["boost"] = 5 } } },
                new JsonObject
                {
                    ["match"] = new JsonObject
                    {
                        ["productName.onek"] = new JsonObject { ["query"] = q, ["fuzziness"] = "AUTO", ["operator"] = "and" }
                    }
                },
                new JsonObject
                {
                    ["multi_match"] = new JsonObject
                    {
                        ["query"] = q,
                        ["fields"] = new JsonArray { "productName^3", "brand^2" },
                        ["fuzziness"] = "AUTO",
                        ["operator"] = "and"
                    }
                }
            };
            kosul["minimum_should_match"] = 1;
            siralama.Add("_score");
        }

        siralama.Add(new JsonObject { ["productName.sirala"] = "asc" });

        var govde = new JsonObject
        {
            ["query"] = new JsonObject { ["bool"] = kosul },
            ["sort"] = siralama,
            ["from"] = (istek.Sayfa - 1) * istek.Adet,
            ["size"] = istek.Adet,
            ["track_total_hits"] = true
        };

        using var icerik = new StringContent(govde.ToJsonString(), Encoding.UTF8, "application/json");
        using var cevap = await http.PostAsync($"{indeks}/_search", icerik);
        await HataVarsaFirlat(cevap);

        using var belge = await JsonDocument.ParseAsync(await cevap.Content.ReadAsStreamAsync());
        var hits = belge.RootElement.GetProperty("hits");
        var sonuc = new UrunAramaSonucu
        {
            Toplam = hits.GetProperty("total").GetProperty("value").GetInt64(),
            Sayfa = istek.Sayfa
        };

        foreach (var hit in hits.GetProperty("hits").EnumerateArray())
        {
            var s = hit.GetProperty("_source");
            sonuc.Urunler.Add(new ProductDto
            {
                ProductId = s.GetProperty("productId").GetInt32(),
                ProductCode = s.GetProperty("productCode").GetString() ?? "",
                ProductName = s.GetProperty("productName").GetString() ?? "",
                Brand = s.TryGetProperty("brand", out var marka) && marka.ValueKind == JsonValueKind.String ? marka.GetString() : null,
                Price = decimal.Parse(s.GetProperty("price").GetRawText(), CultureInfo.InvariantCulture),
                StockQuantity = s.GetProperty("stockQuantity").GetInt32(),
                IsActive = s.GetProperty("isActive").GetBoolean()
            });
        }

        return sonuc;
    }

    private static async Task HataVarsaFirlat(HttpResponseMessage cevap)
    {
        if (!cevap.IsSuccessStatusCode)
        {
            string mesaj = await cevap.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Elasticsearch {(int)cevap.StatusCode}: {mesaj}");
        }
    }
}
