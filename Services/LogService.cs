using System.Security.Claims;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;

namespace c1Soft_b4bProje.Services;

public class LogService
{
    private readonly IServiceScopeFactory scopeFactory;

    public LogService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    // Logu isteğin kendi db'sine karışmasın diye ayrı bir bağlantıyla kaydeder
    public async Task YazAsync(IslemLog log)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        log.Aciklama = Kisalt(log.Aciklama, 500);
        log.Yol = Kisalt(log.Yol, 300);

        db.IslemLog.Add(log);
        await db.SaveChangesAsync();
    }

    // İstekten domain ve ip bilgisini doldurup log yazar
    public Task YazAsync(HttpContext context, string tur, string? aciklama,
        int? kullaniciId = null, int? firmaId = null, string? kulAdi = null)
    {
        return YazAsync(new IslemLog
        {
            Tarih = DateTime.Now,
            Tur = tur,
            KullaniciId = kullaniciId,
            FirmaId = firmaId,
            KulAdi = kulAdi,
            Metot = context.Request.Method,
            Yol = context.Request.Path,
            Domain = OturumService.DomainBul(context),
            IpAdresi = context.Connection.RemoteIpAddress?.ToString(),
            Aciklama = aciklama
        });
    }

    // Yakalanmayan hatayı istek bilgileriyle birlikte kaydeder, hata numarasını döner
    public async Task<int?> HataYazAsync(HttpContext context, Exception hata)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var kayit = new HataLog
            {
                Tarih = DateTime.Now,
                KullaniciId = SayiOku(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                FirmaId = SayiOku(context.User.FindFirst(TokenService.FirmaIdClaim)?.Value),
                KulAdi = Kisalt(context.User.Identity?.Name, 50),
                Metot = context.Request.Method,
                Yol = Kisalt(context.Request.Path + context.Request.QueryString, 300),
                Domain = Kisalt(OturumService.DomainBul(context), 200),
                IpAdresi = context.Connection.RemoteIpAddress?.ToString(),
                HataTipi = Kisalt(hata.GetType().FullName, 200) ?? "Exception",
                Mesaj = Kisalt(hata.Message, 1000) ?? "",
                Detay = hata.ToString()
            };

            db.HataLog.Add(kayit);
            await db.SaveChangesAsync();

            return kayit.HataId;
        }
        catch
        {
            return null;
        }
    }

    // Metni sayıya çevirir, olmazsa null döner
    private static int? SayiOku(string? deger)
    {
        return int.TryParse(deger, out int sayi) ? sayi : null;
    }

    // Metni veritabanı kolonuna sığacak uzunlukta keser
    private static string? Kisalt(string? metin, int uzunluk)
    {
        if (metin == null || metin.Length <= uzunluk)
        {
            return metin;
        }

        return metin.Substring(0, uzunluk);
    }
}
