using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class HatalarController : AdminController
{
    public HatalarController(ApplicationDbContext db, LogService logService) : base(db, logService)
    {
    }

    // Sistemde oluşan hataları çözülme durumuna ve tarihe göre listeler
    public async Task<IActionResult> Index([FromQuery] FiltreSecenegi? filtre = null, int sayfa = 1)
    {
        filtre ??= new FiltreSecenegi();
        filtre.Durum ??= "acik";
        ViewBag.Filtre = filtre;

        var sorgu = db.HataLog.AsQueryable();

        if (filtre.Durum == "acik")
        {
            sorgu = sorgu.Where(x => !x.IsCozuldu);
        }
        else if (filtre.Durum == "cozuldu")
        {
            sorgu = sorgu.Where(x => x.IsCozuldu);
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
            sorgu = sorgu.Where(x => x.Mesaj.Contains(filtre.Arama) || x.Yol!.Contains(filtre.Arama) || x.HataTipi.Contains(filtre.Arama));
        }

        var model = await SayfaliListe<HataLog>.OlusturAsync(sorgu.OrderByDescending(x => x.HataId), sayfa, 25);
        return View(model);
    }

    // Hatayı çözüldü ya da tekrar açık olarak işaretler
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id, string? returnUrl)
    {
        var hata = await db.HataLog.FirstOrDefaultAsync(x => x.HataId == id);

        if (hata == null)
        {
            return NotFound();
        }

        hata.IsCozuldu = !hata.IsCozuldu;
        hata.CozulmeTarihi = hata.IsCozuldu ? DateTime.Now : null;
        await db.SaveChangesAsync();

        await AdminLogYaz($"{hata.HataId} numaralı hata {(hata.IsCozuldu ? "çözüldü" : "tekrar açıldı")} olarak işaretlendi");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index");
    }
}
