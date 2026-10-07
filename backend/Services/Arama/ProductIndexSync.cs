using System.Runtime.CompilerServices;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace c1Soft_b4bProje.Services.Arama;

public class ProductIndexSync
{
    private readonly ProductSearchIndex indeks;
    private readonly SearchCache cache;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ProductIndexSync> logger;

    public ProductIndexSync(ProductSearchIndex indeks, SearchCache cache, IServiceScopeFactory scopeFactory, ILogger<ProductIndexSync> logger)
    {
        this.indeks = indeks;
        this.cache = cache;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    // Değişen ürünleri indekse yazar ve firmaların önbelleğini geçersiz yapar
    public async Task UygulaAsync(IReadOnlyList<(Product Urun, bool Silindi)> degisenler)
    {
        try
        {
            await indeks.TopluYazAsync(
                degisenler.Where(x => !x.Silindi).Select(x => x.Urun),
                degisenler.Where(x => x.Silindi).Select(x => x.Urun.ProductId));
        }
        catch (Exception hata) when (hata is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(hata, "Ürün değişikliği arama indeksine yazılamadı");
        }

        foreach (int firmaId in degisenler.Select(x => x.Urun.FirmaId).Distinct())
        {
            await cache.SurumArttirAsync(firmaId);
        }
    }

    // Tüm firmaların ürünlerini indekse baştan yükler
    public async Task TumunuYukleAsync()
    {
        await indeks.YenidenKurAsync();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        int sonId = 0;

        while (true)
        {
            var parca = await db.Products.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.ProductId > sonId)
                .OrderBy(x => x.ProductId)
                .Take(2000)
                .ToListAsync();

            if (parca.Count == 0)
            {
                break;
            }

            await indeks.TopluYazAsync(parca, Array.Empty<int>());
            sonId = parca[^1].ProductId;
        }

        foreach (int firmaId in await db.Firma.IgnoreQueryFilters().Select(x => x.FirmaId).ToListAsync())
        {
            await cache.SurumArttirAsync(firmaId);
        }
    }
}

public class ProductIndexInterceptor : SaveChangesInterceptor
{
    private readonly ProductIndexSync sync;
    private readonly ConditionalWeakTable<DbContext, List<(Product, bool)>> bekleyenler = new();

    public ProductIndexInterceptor(ProductIndexSync sync)
    {
        this.sync = sync;
    }

    // Kayıttan önce değişen ürünleri not alır
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            var liste = eventData.Context.ChangeTracker.Entries<Product>()
                .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(x => (x.Entity, x.State == EntityState.Deleted))
                .ToList();

            if (liste.Count > 0)
            {
                bekleyenler.AddOrUpdate(eventData.Context, liste);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Kayıt başarılı olunca değişiklikleri arama indeksine gönderir
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null && bekleyenler.TryGetValue(eventData.Context, out var liste))
        {
            bekleyenler.Remove(eventData.Context);
            await sync.UygulaAsync(liste);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // Kayıt hata verirse not alınanları bırakır
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            bekleyenler.Remove(eventData.Context);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}

public class AramaIndeksHazirlayici : BackgroundService
{
    private readonly ProductSearchIndex indeks;
    private readonly ProductIndexSync sync;
    private readonly ILogger<AramaIndeksHazirlayici> logger;

    public AramaIndeksHazirlayici(ProductSearchIndex indeks, ProductIndexSync sync, ILogger<AramaIndeksHazirlayici> logger)
    {
        this.indeks = indeks;
        this.sync = sync;
        this.logger = logger;
    }

    // Uygulama açılınca indeks yoksa kurup ürünleri yükler
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!await indeks.VarMiAsync())
            {
                await sync.TumunuYukleAsync();
                logger.LogInformation("Arama indeksi kuruldu");
            }
        }
        catch (Exception hata)
        {
            logger.LogWarning(hata, "Arama indeksi hazırlanamadı, arama SQL ile çalışacak");
        }
    }
}
