using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class OturumlarController : AdminController
{
    private readonly OturumService oturumService;

    public OturumlarController(ApplicationDbContext db, LogService logService, OturumService oturumService)
        : base(db, logService)
    {
        this.oturumService = oturumService;
    }

    // Aktif oturumları ya da farklı domain yüzünden düşürülen oturumları listeler
    public async Task<IActionResult> Index(string durum = "aktif", int? firmaId = null, int sayfa = 1)
    {
        ViewBag.Durum = durum;
        ViewBag.FirmaId = firmaId;
        await FirmalariYukle(firmaId);

        var sorgu = db.KullaniciOturum.AsQueryable();

        if (firmaId.HasValue)
        {
            sorgu = sorgu.Where(x => x.FirmaId == firmaId);
        }

        sorgu = durum switch
        {
            "dusurulen" => sorgu.Where(x => !x.IsAktif && x.KapanmaNedeni!.StartsWith("Farklı domainden giriş")),
            "hepsi" => sorgu,
            _ => sorgu.Where(x => x.IsAktif)
        };

        var model = await SayfaliListe<OturumSatiri>.OlusturAsync(
            OturumSatirlari(sorgu).OrderByDescending(x => x.GirisTarihi), sayfa);

        return View(model);
    }

    // Seçilen oturumu yönetici olarak kapatır, kullanıcı tekrar giriş yapmak zorunda kalır
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kapat(int id)
    {
        var oturum = await db.KullaniciOturum
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.OturumId == id);

        if (oturum == null)
        {
            return NotFound();
        }

        if (await oturumService.KapatAsync(oturum.OturumAnahtari, $"Yönetici tarafından kapatıldı ({User.Identity?.Name})"))
        {
            await AdminLogYaz($"{oturum.Kullanici?.KulAdi} kullanıcısının oturumu kapatıldı ({oturum.Domain})");
            TempData["Mesaj"] = $"{oturum.Kullanici?.KulAdi} kullanıcısının oturumu kapatıldı.";
        }

        return RedirectToAction("Index");
    }
}
