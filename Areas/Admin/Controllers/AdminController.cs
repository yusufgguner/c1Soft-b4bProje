using System.Security.Claims;
using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AdminPanel.Sema, Roles = Roller.SistemAdmin)]
public abstract class AdminController : Controller
{
    protected readonly ApplicationDbContext db;
    protected readonly LogService logService;

    protected AdminController(ApplicationDbContext db, LogService logService)
    {
        this.db = db;
        this.logService = logService;
    }

    // Filtre kutusundaki firma listesini hazırlar
    protected async Task FirmalariYukle(int? seciliFirmaId)
    {
        var firmalar = await db.Firma
            .OrderBy(x => x.FirmaKodu)
            .Select(x => new { x.FirmaId, Ad = x.FirmaKodu + " - " + x.Adi })
            .ToListAsync();

        ViewBag.Firmalar = new SelectList(firmalar, "FirmaId", "Ad", seciliFirmaId);
    }

    // Yöneticinin panelde yaptığı işlemi loglar
    protected Task AdminLogYaz(string aciklama)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int kullaniciId);

        return logService.YazAsync(HttpContext, LogTurleri.Admin, aciklama,
            kullaniciId == 0 ? null : kullaniciId, null, User.Identity?.Name);
    }

    // Bitiş tarihini o günün sonuna kadar kapsayacak şekilde döner
    protected static DateTime? GunSonu(DateTime? bitis)
    {
        return bitis?.Date.AddDays(1);
    }

    // Kullanıcı oturumlarını tabloda gösterilecek satırlara çevirir
    protected static IQueryable<OturumSatiri> OturumSatirlari(IQueryable<KullaniciOturum> sorgu)
    {
        return sorgu.Select(x => new OturumSatiri
        {
            OturumId = x.OturumId,
            KulAdi = x.Kullanici!.KulAdi,
            AdSoyad = x.Kullanici.AdSoyad,
            FirmaKodu = x.Firma!.FirmaKodu,
            Domain = x.Domain,
            IpAdresi = x.IpAdresi,
            Tarayici = x.Tarayici,
            GirisTarihi = x.GirisTarihi,
            SonIslemTarihi = x.SonIslemTarihi,
            CikisTarihi = x.CikisTarihi,
            IsAktif = x.IsAktif,
            KapanmaNedeni = x.KapanmaNedeni
        });
    }

    // Siparişleri tüm firmalar için tabloda gösterilecek satırlara çevirir
    protected IQueryable<SiparisSatiri> SiparisSatirlari()
    {
        return db.SiparisR
            .IgnoreQueryFilters()
            .Select(x => new SiparisSatiri
            {
                SiparisId = x.SiparisId,
                SiparisNo = x.SiparisNo,
                FirmaId = x.FirmaId,
                FirmaKodu = x.Kullanici!.Firma!.FirmaKodu,
                FirmaAdi = x.Kullanici.Firma.Adi,
                KulAdi = x.Kullanici.KulAdi,
                Tarih = x.Tarih,
                GenelTutar = x.GenelTutar,
                SiparisDurumu = x.SiparisDurumu,
                KalemSayisi = x.Kalemler.Count
            });
    }
}
