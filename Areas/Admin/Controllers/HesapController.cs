using System.Security.Claims;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Controllers;

[Area("Admin")]
public class HesapController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly PasswordService passwordService;
    private readonly LogService logService;

    public HesapController(ApplicationDbContext db, PasswordService passwordService, LogService logService)
    {
        this.db = db;
        this.passwordService = passwordService;
        this.logService = logService;
    }

    // Yönetim paneli giriş sayfasını açar
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Giris(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // Süper admin kullanıcı adı ve şifresini kontrol edip panele giriş yapar
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(string kulAdi, string sifre, string? returnUrl)
    {
        var kullanici = await db.Kullanici
            .Include(x => x.Firma)
            .FirstOrDefaultAsync(x => x.KulAdi == kulAdi && x.Rol == Roller.SistemAdmin && x.IsAktif);

        if (kullanici == null || !passwordService.Verify(sifre ?? "", kullanici.SifreHash))
        {
            await logService.YazAsync(HttpContext, LogTurleri.HataliGiris, $"Yönetim paneline hatalı giriş: {kulAdi}",
                kullanici?.KullaniciId, kullanici?.FirmaId, kulAdi);

            ViewBag.Hata = "Kullanıcı adı veya şifre hatalı.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KulAdi),
            new Claim(ClaimTypes.GivenName, kullanici.AdSoyad),
            new Claim(ClaimTypes.Role, kullanici.Rol)
        };

        var kimlik = new ClaimsIdentity(claims, AdminPanel.Sema);
        await HttpContext.SignInAsync(AdminPanel.Sema, new ClaimsPrincipal(kimlik));

        kullanici.SonGirisTarihi = DateTime.Now;
        await db.SaveChangesAsync();

        await logService.YazAsync(HttpContext, LogTurleri.Giris, "Yönetim paneline giriş yapıldı",
            kullanici.KullaniciId, kullanici.FirmaId, kullanici.KulAdi);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Panel");
    }

    // Panelden çıkış yapar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = AdminPanel.Sema)]
    public async Task<IActionResult> Cikis()
    {
        await logService.YazAsync(HttpContext, LogTurleri.Cikis, "Yönetim panelinden çıkış yapıldı",
            null, null, User.Identity?.Name);

        await HttpContext.SignOutAsync(AdminPanel.Sema);
        return RedirectToAction("Giris");
    }
}
