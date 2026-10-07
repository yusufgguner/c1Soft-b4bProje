using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class LoglarController : AdminController
{
    public LoglarController(ApplicationDbContext db, LogService logService) : base(db, logService)
    {
    }

    // Kullanıcıların yaptığı bütün işlemleri filtreyle listeler
    public async Task<IActionResult> Index([FromQuery] FiltreSecenegi? filtre = null, int sayfa = 1)
    {
        filtre ??= new FiltreSecenegi();
        ViewBag.Filtre = filtre;
        await FirmalariYukle(filtre.FirmaId);

        var sorgu = db.IslemLog.AsQueryable();

        if (filtre.FirmaId.HasValue)
        {
            sorgu = sorgu.Where(x => x.FirmaId == filtre.FirmaId);
        }

        if (!string.IsNullOrEmpty(filtre.Tur))
        {
            sorgu = sorgu.Where(x => x.Tur == filtre.Tur);
        }

        if (filtre.Baslangic.HasValue)
        {
            sorgu = sorgu.Where(x => x.Tarih >= filtre.Baslangic);
        }

        DateTime? bitis = GunSonu(filtre.Bitis);

        if (bitis.HasValue)
        {
            sorgu = sorgu.Where(x => x.Tarih < bitis);
        }

        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            sorgu = sorgu.Where(x =>
                x.KulAdi!.Contains(filtre.Arama) ||
                x.Yol!.Contains(filtre.Arama) ||
                x.Domain!.Contains(filtre.Arama) ||
                x.IpAdresi!.Contains(filtre.Arama));
        }

        ViewBag.Turler = LogTurleri.Hepsi;

        var model = await SayfaliListe<IslemLog>.OlusturAsync(sorgu.OrderByDescending(x => x.LogId), sayfa);
        return View(model);
    }
}
