using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class PanelController : AdminController
{
    public PanelController(ApplicationDbContext db, LogService logService) : base(db, logService)
    {
    }

    // Ana sayfadaki sayıları, son siparişleri ve son girişleri getirir
    public async Task<IActionResult> Index()
    {
        DateTime bugun = DateTime.Today;
        var bugunkuSiparisler = db.SiparisR.IgnoreQueryFilters().Where(x => x.Tarih >= bugun);

        var model = new PanelOzet
        {
            FirmaSayisi = await db.Firma.CountAsync(x => x.IsAktif),
            KullaniciSayisi = await db.Kullanici.CountAsync(x => x.IsAktif),
            BugunGiris = await db.KullaniciOturum.CountAsync(x => x.GirisTarihi >= bugun),
            AktifOturum = await db.KullaniciOturum.CountAsync(x => x.IsAktif),
            BugunDusurulenOturum = await db.KullaniciOturum.CountAsync(x =>
                x.CikisTarihi >= bugun && x.KapanmaNedeni!.StartsWith("Farklı domainden giriş")),
            BugunSiparis = await bugunkuSiparisler.CountAsync(),
            BugunCiro = await bugunkuSiparisler
                .Where(x => x.SiparisDurumu != "Cancelled" && x.SiparisDurumu != "Rejected")
                .SumAsync(x => (decimal?)x.GenelTutar) ?? 0,
            SonSiparisler = await SiparisSatirlari().OrderByDescending(x => x.Tarih).Take(10).ToListAsync(),
            SonGirisler = await OturumSatirlari(db.KullaniciOturum).OrderByDescending(x => x.GirisTarihi).Take(8).ToListAsync()
        };

        return View(model);
    }
}
