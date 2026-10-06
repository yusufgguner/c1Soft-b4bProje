using c1Soft_b4bProje.Areas.Admin.Models;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Mvc;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

public class KullanicilarController : AdminController
{
    public KullanicilarController(ApplicationDbContext db, LogService logService) : base(db, logService)
    {
    }

    // Sisteme giriş yapan kullanıcıları ya da yeni kayıt olan kullanıcıları listeler
    public async Task<IActionResult> Index(string sekme = "girisler", [FromQuery] FiltreSecenegi? filtre = null, int sayfa = 1)
    {
        filtre ??= new FiltreSecenegi();
        ViewBag.Sekme = sekme == "kayitlar" ? "kayitlar" : "girisler";
        ViewBag.Filtre = filtre;
        await FirmalariYukle(filtre.FirmaId);

        DateTime? bitis = GunSonu(filtre.Bitis);

        if (ViewBag.Sekme == "kayitlar")
        {
            var kayitlar = db.Kullanici.AsQueryable();

            if (filtre.FirmaId.HasValue)
            {
                kayitlar = kayitlar.Where(x => x.FirmaId == filtre.FirmaId);
            }

            if (filtre.Baslangic.HasValue)
            {
                kayitlar = kayitlar.Where(x => x.OlusturmaTarihi >= filtre.Baslangic);
            }

            if (bitis.HasValue)
            {
                kayitlar = kayitlar.Where(x => x.OlusturmaTarihi < bitis);
            }

            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                kayitlar = kayitlar.Where(x => x.KulAdi.Contains(filtre.Arama) || x.AdSoyad.Contains(filtre.Arama));
            }

            var satirlar = kayitlar
                .OrderByDescending(x => x.OlusturmaTarihi)
                .Select(x => new KullaniciSatiri
                {
                    KullaniciId = x.KullaniciId,
                    KulAdi = x.KulAdi,
                    AdSoyad = x.AdSoyad,
                    Email = x.Email,
                    Rol = x.Rol,
                    FirmaKodu = x.Firma!.FirmaKodu,
                    FirmaAdi = x.Firma.Adi,
                    IsAktif = x.IsAktif,
                    OlusturmaTarihi = x.OlusturmaTarihi,
                    SonGirisTarihi = x.SonGirisTarihi
                });

            ViewBag.Kayitlar = await SayfaliListe<KullaniciSatiri>.OlusturAsync(satirlar, sayfa);
            return View();
        }

        var girisler = db.KullaniciOturum.AsQueryable();

        if (filtre.FirmaId.HasValue)
        {
            girisler = girisler.Where(x => x.FirmaId == filtre.FirmaId);
        }

        if (filtre.Baslangic.HasValue)
        {
            girisler = girisler.Where(x => x.GirisTarihi >= filtre.Baslangic);
        }

        if (bitis.HasValue)
        {
            girisler = girisler.Where(x => x.GirisTarihi < bitis);
        }

        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            girisler = girisler.Where(x => x.Kullanici!.KulAdi.Contains(filtre.Arama) || x.Domain!.Contains(filtre.Arama));
        }

        ViewBag.Girisler = await SayfaliListe<OturumSatiri>.OlusturAsync(
            OturumSatirlari(girisler).OrderByDescending(x => x.GirisTarihi), sayfa);

        return View();
    }
}
