using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class SiparislerController : AdminController
{
    public static readonly string[] Durumlar = { "Pending", "Approved", "Rejected", "Shipped", "Delivered", "Cancelled" };

    public SiparislerController(ApplicationDbContext db, LogService logService) : base(db, logService)
    {
    }

    // Bütün firmaların siparişlerini filtreyle listeler, yeni siparişler sayfaya anlık düşer
    public async Task<IActionResult> Index([FromQuery] FiltreSecenegi? filtre = null, int sayfa = 1)
    {
        filtre ??= new FiltreSecenegi();
        ViewBag.Filtre = filtre;
        await FirmalariYukle(filtre.FirmaId);

        var sorgu = SiparisSatirlari();

        if (filtre.FirmaId.HasValue)
        {
            sorgu = sorgu.Where(x => x.FirmaId == filtre.FirmaId);
        }

        if (!string.IsNullOrEmpty(filtre.Durum))
        {
            sorgu = sorgu.Where(x => x.SiparisDurumu == filtre.Durum);
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
            sorgu = sorgu.Where(x => x.SiparisNo.Contains(filtre.Arama) || x.KulAdi.Contains(filtre.Arama));
        }

        var model = await SayfaliListe<SiparisSatiri>.OlusturAsync(sorgu.OrderByDescending(x => x.Tarih), sayfa);
        return View(model);
    }

    // Siparişin başlık ve kalem bilgilerini gösterir
    public async Task<IActionResult> Detay(int id)
    {
        var siparis = await db.SiparisR
            .IgnoreQueryFilters()
            .Include(x => x.Kalemler)
            .Include(x => x.Kullanici)
                .ThenInclude(x => x!.Firma)
            .FirstOrDefaultAsync(x => x.SiparisId == id);

        if (siparis == null)
        {
            return NotFound();
        }

        return View(siparis);
    }
}
