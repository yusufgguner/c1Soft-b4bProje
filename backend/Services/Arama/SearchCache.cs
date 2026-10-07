using System.Globalization;
using System.Text.Json;
using c1Soft_b4bProje.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace c1Soft_b4bProje.Services.Arama;

public class SearchCache
{
    private readonly IMemoryCache bellek;
    private readonly IConnectionMultiplexer redis;
    private readonly TimeSpan bellekSuresi;
    private readonly TimeSpan redisSuresi;

    public SearchCache(IMemoryCache bellek, IConnectionMultiplexer redis, IOptions<AramaAyarlari> ayarlar)
    {
        this.bellek = bellek;
        this.redis = redis;
        bellekSuresi = TimeSpan.FromSeconds(ayarlar.Value.BellekSaniye);
        redisSuresi = TimeSpan.FromMinutes(ayarlar.Value.RedisDakika);
    }

    private bool RedisHazir => redis.IsConnected;

    // Firmanın önbellek sürümünü getirir, ürün değişince bu sayı artar
    public async Task<long> SurumAsync(int firmaId)
    {
        string anahtar = SurumAnahtari(firmaId);

        if (bellek.TryGetValue(anahtar, out long surum))
        {
            return surum;
        }

        if (RedisHazir)
        {
            try
            {
                var deger = await redis.GetDatabase().StringGetAsync(anahtar);
                surum = deger.HasValue ? (long)deger : 0;
            }
            catch (RedisException)
            {
                surum = 0;
            }
        }

        bellek.Set(anahtar, surum, TimeSpan.FromSeconds(1));
        return surum;
    }

    // Arama isteğinden önbellek anahtarı üretir
    public string Anahtar(int firmaId, long surum, UrunAramaIstegi i)
    {
        return string.Join('|',
            "ara", firmaId.ToString(CultureInfo.InvariantCulture), surum.ToString(CultureInfo.InvariantCulture),
            (i.Q ?? "").Trim().ToLowerInvariant(),
            (i.Marka ?? "").Trim().ToLowerInvariant(),
            i.MinFiyat?.ToString(CultureInfo.InvariantCulture) ?? "",
            i.MaxFiyat?.ToString(CultureInfo.InvariantCulture) ?? "",
            i.StoktaOlan ? "1" : "0",
            i.SadeceAktif ? "1" : "0",
            i.Sayfa.ToString(CultureInfo.InvariantCulture),
            i.Adet.ToString(CultureInfo.InvariantCulture));
    }

    // Sonucu uygulama belleğinden okur
    public bool BellektenAl(string anahtar, out UrunAramaSonucu? sonuc)
    {
        return bellek.TryGetValue(anahtar, out sonuc);
    }

    // Sonucu Redis'ten okur ve belleğe de koyar
    public async Task<UrunAramaSonucu?> RedistenAlAsync(string anahtar)
    {
        if (!RedisHazir)
        {
            return null;
        }

        try
        {
            var deger = await redis.GetDatabase().StringGetAsync(anahtar);

            if (!deger.HasValue)
            {
                return null;
            }

            var sonuc = JsonSerializer.Deserialize<UrunAramaSonucu>((string)deger!);

            if (sonuc != null)
            {
                bellek.Set(anahtar, sonuc, bellekSuresi);
            }

            return sonuc;
        }
        catch (RedisException)
        {
            return null;
        }
    }

    // Sonucu belleğe ve Redis'e yazar
    public async Task YazAsync(string anahtar, UrunAramaSonucu sonuc)
    {
        bellek.Set(anahtar, sonuc, bellekSuresi);

        if (!RedisHazir)
        {
            return;
        }

        try
        {
            await redis.GetDatabase().StringSetAsync(anahtar, JsonSerializer.Serialize(sonuc), redisSuresi);
        }
        catch (RedisException)
        {
        }
    }

    // Firmanın sürümünü artırır, eski aramalar geçersiz olur
    public async Task SurumArttirAsync(int firmaId)
    {
        string anahtar = SurumAnahtari(firmaId);
        long yeni = bellek.TryGetValue(anahtar, out long eski) ? eski + 1 : 1;

        if (RedisHazir)
        {
            try
            {
                yeni = await redis.GetDatabase().StringIncrementAsync(anahtar);
            }
            catch (RedisException)
            {
            }
        }

        bellek.Set(anahtar, yeni, TimeSpan.FromSeconds(1));
    }

    private static string SurumAnahtari(int firmaId) => $"ara-surum|{firmaId}";
}
