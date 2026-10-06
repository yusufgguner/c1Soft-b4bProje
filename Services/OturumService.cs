using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Services;

public class OturumService
{
    public const string OturumClaim = "OturumId";

    private readonly ApplicationDbContext db;
    private readonly LogService logService;

    public OturumService(ApplicationDbContext db, LogService logService)
    {
        this.db = db;
        this.logService = logService;
    }

    // İsteğin hangi domainden geldiğini Origin, Referer ya da Host bilgisinden bulur
    public static string DomainBul(HttpContext context)
    {
        string? origin = context.Request.Headers.Origin.FirstOrDefault();

        if (Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
        {
            return originUri.Host;
        }

        string? referer = context.Request.Headers.Referer.FirstOrDefault();

        if (Uri.TryCreate(referer, UriKind.Absolute, out Uri? refererUri))
        {
            return refererUri.Host;
        }

        return context.Request.Host.Host;
    }

    // Kullanıcının açık oturumlarını kapatıp yeni oturum açar, farklı domainden girdiyse loglar
    public async Task<KullaniciOturum> OturumAcAsync(Kullanici kullanici, HttpContext context)
    {
        string domain = DomainBul(context);

        var acikOturumlar = await db.KullaniciOturum
            .Where(x => x.KullaniciId == kullanici.KullaniciId && x.IsAktif)
            .ToListAsync();

        foreach (var eski in acikOturumlar)
        {
            bool farkliDomain = !string.Equals(eski.Domain, domain, StringComparison.OrdinalIgnoreCase);

            eski.IsAktif = false;
            eski.CikisTarihi = DateTime.Now;
            eski.KapanmaNedeni = farkliDomain
                ? $"Farklı domainden giriş: {eski.Domain} -> {domain}"
                : $"Yeni giriş: {domain}";

            if (farkliDomain)
            {
                await logService.YazAsync(context, LogTurleri.OturumDusuruldu, eski.KapanmaNedeni,
                    kullanici.KullaniciId, kullanici.FirmaId, kullanici.KulAdi);
            }
        }

        var oturum = new KullaniciOturum
        {
            KullaniciId = kullanici.KullaniciId,
            FirmaId = kullanici.FirmaId,
            OturumAnahtari = Guid.NewGuid(),
            Domain = domain,
            IpAdresi = context.Connection.RemoteIpAddress?.ToString(),
            Tarayici = Kisalt(context.Request.Headers.UserAgent.ToString(), 300),
            GirisTarihi = DateTime.Now,
            SonIslemTarihi = DateTime.Now,
            IsAktif = true
        };

        db.KullaniciOturum.Add(oturum);
        await db.SaveChangesAsync();

        return oturum;
    }

    // Token'daki oturum hâlâ açık mı diye bakar, açıksa son işlem zamanını günceller
    public async Task<bool> AktifMiAsync(Guid oturumAnahtari)
    {
        var oturum = await db.KullaniciOturum
            .AsNoTracking()
            .Where(x => x.OturumAnahtari == oturumAnahtari)
            .Select(x => new { x.OturumId, x.IsAktif, x.SonIslemTarihi })
            .FirstOrDefaultAsync();

        if (oturum == null || !oturum.IsAktif)
        {
            return false;
        }

        if (oturum.SonIslemTarihi < DateTime.Now.AddMinutes(-1))
        {
            await db.KullaniciOturum
                .Where(x => x.OturumId == oturum.OturumId)
                .ExecuteUpdateAsync(x => x.SetProperty(o => o.SonIslemTarihi, DateTime.Now));
        }

        return true;
    }

    // Oturumu verilen sebeple kapatır
    public async Task<bool> KapatAsync(Guid oturumAnahtari, string neden)
    {
        int adet = await db.KullaniciOturum
            .Where(x => x.OturumAnahtari == oturumAnahtari && x.IsAktif)
            .ExecuteUpdateAsync(x => x
                .SetProperty(o => o.IsAktif, false)
                .SetProperty(o => o.CikisTarihi, DateTime.Now)
                .SetProperty(o => o.KapanmaNedeni, neden));

        return adet > 0;
    }

    // Metni kolona sığacak uzunlukta keser
    private static string? Kisalt(string? metin, int uzunluk)
    {
        if (string.IsNullOrEmpty(metin))
        {
            return null;
        }

        return metin.Length <= uzunluk ? metin : metin.Substring(0, uzunluk);
    }
}
