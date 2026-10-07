using System.Diagnostics;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Services.Arama;

public class ProductSearchService
{
    private readonly ProductSearchIndex indeks;
    private readonly SearchCache cache;
    private readonly ApplicationDbContext db;
    private readonly ILogger<ProductSearchService> logger;

    public ProductSearchService(ProductSearchIndex indeks, SearchCache cache, ApplicationDbContext db, ILogger<ProductSearchService> logger)
    {
        this.indeks = indeks;
        this.cache = cache;
        this.db = db;
        this.logger = logger;
    }

    // Sırayla bellek, Redis ve Elasticsearch'e bakar, olmazsa SQL'den arar
    public async Task<(UrunAramaSonucu Sonuc, string Kaynak, double Ms)> AraAsync(int firmaId, UrunAramaIstegi istek)
    {
        var sure = Stopwatch.StartNew();
        istek.Sayfa = Math.Max(1, istek.Sayfa);
        istek.Adet = Math.Clamp(istek.Adet, 1, 100);

        long surum = await cache.SurumAsync(firmaId);
        string anahtar = cache.Anahtar(firmaId, surum, istek);

        if (cache.BellektenAl(anahtar, out var bellekte) && bellekte != null)
        {
            return (bellekte, "memory", sure.Elapsed.TotalMilliseconds);
        }

        var redisten = await cache.RedistenAlAsync(anahtar);

        if (redisten != null)
        {
            return (redisten, "redis", sure.Elapsed.TotalMilliseconds);
        }

        try
        {
            var sonuc = await indeks.AraAsync(firmaId, istek);
            await cache.YazAsync(anahtar, sonuc);
            return (sonuc, "elastic", sure.Elapsed.TotalMilliseconds);
        }
        catch (Exception hata) when (hata is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(hata, "Elasticsearch'e ulaşılamadı, SQL ile aranıyor");
            var sonuc = await SqldenAraAsync(istek);
            return (sonuc, "sql", sure.Elapsed.TotalMilliseconds);
        }
    }

    // Elasticsearch yokken aynı filtrelerle SQL'den arar
    private async Task<UrunAramaSonucu> SqldenAraAsync(UrunAramaIstegi istek)
    {
        var sorgu = db.Products.AsNoTracking().AsQueryable();
        string? q = istek.Q?.Trim();

        if (istek.SadeceAktif) sorgu = sorgu.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(istek.Marka)) sorgu = sorgu.Where(x => x.Brand == istek.Marka);
        if (istek.MinFiyat != null) sorgu = sorgu.Where(x => x.Price >= istek.MinFiyat);
        if (istek.MaxFiyat != null) sorgu = sorgu.Where(x => x.Price <= istek.MaxFiyat);
        if (istek.StoktaOlan) sorgu = sorgu.Where(x => x.StockQuantity > 0);

        if (!string.IsNullOrEmpty(q))
        {
            sorgu = sorgu.Where(x =>
                x.ProductCode.Contains(q) ||
                x.ProductName.Contains(q) ||
                (x.Brand != null && x.Brand.Contains(q)));
        }

        var sonuc = new UrunAramaSonucu { Toplam = await sorgu.CountAsync(), Sayfa = istek.Sayfa };
        sonuc.Urunler = await sorgu
            .OrderBy(x => x.ProductName)
            .Skip((istek.Sayfa - 1) * istek.Adet)
            .Take(istek.Adet)
            .Select(x => new ProductDto
            {
                ProductId = x.ProductId,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                Brand = x.Brand,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return sonuc;
    }
}
