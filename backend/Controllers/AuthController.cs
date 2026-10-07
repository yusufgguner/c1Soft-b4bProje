using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext db;
    private readonly PasswordService passwordService;
    private readonly TokenService tokenService;
    private readonly TenantService tenant;
    private readonly OturumService oturumService;
    private readonly LogService logService;

    public AuthController(ApplicationDbContext db, PasswordService passwordService,
        TokenService tokenService, TenantService tenant, OturumService oturumService, LogService logService)
    {
        this.db = db;
        this.passwordService = passwordService;
        this.tokenService = tokenService;
        this.tenant = tenant;
        this.oturumService = oturumService;
        this.logService = logService;
    }

    // Kullanıcı adı ve şifreyle giriş yapıp token döner
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest model)
    {
        var kullanici = await db.Kullanici
            .Include(x => x.Firma)
            .FirstOrDefaultAsync(x =>
                x.Firma!.FirmaKodu == model.FirmaKodu &&
                x.KulAdi == model.KulAdi &&
                x.IsAktif);

        if (kullanici == null || !passwordService.Verify(model.Sifre, kullanici.SifreHash))
        {
            await logService.YazAsync(HttpContext, LogTurleri.HataliGiris,
                $"Hatalı giriş denemesi: {model.FirmaKodu} / {model.KulAdi}",
                kullanici?.KullaniciId, kullanici?.FirmaId, model.KulAdi);

            return Unauthorized(new { mesaj = "Kullanıcı adı veya şifre hatalı." });
        }

        if (!kullanici.Firma!.IsAktif)
        {
            await logService.YazAsync(HttpContext, LogTurleri.HataliGiris, "Pasif firma ile giriş denemesi",
                kullanici.KullaniciId, kullanici.FirmaId, kullanici.KulAdi);

            return Unauthorized(new { mesaj = "Firmanız pasif durumda, giriş yapılamaz." });
        }

        kullanici.SonGirisTarihi = DateTime.Now;
        var oturum = await oturumService.OturumAcAsync(kullanici, HttpContext);

        await logService.YazAsync(HttpContext, LogTurleri.Giris, $"Giriş yapıldı ({oturum.Domain})",
            kullanici.KullaniciId, kullanici.FirmaId, kullanici.KulAdi);

        var (token, bitisTarihi) = tokenService.TokenOlustur(kullanici, kullanici.Firma, oturum.OturumAnahtari);

        return new LoginResponse
        {
            Token = token,
            BitisTarihi = bitisTarihi,
            KullaniciId = kullanici.KullaniciId,
            KulAdi = kullanici.KulAdi,
            AdSoyad = kullanici.AdSoyad,
            Rol = kullanici.Rol,
            FirmaId = kullanici.FirmaId,
            FirmaKodu = kullanici.Firma.FirmaKodu,
            FirmaAdi = kullanici.Firma.Adi
        };
    }

    // Kullanıcının oturumunu kapatır, token bir daha kullanılamaz
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        string? deger = User.FindFirst(OturumService.OturumClaim)?.Value;

        if (Guid.TryParse(deger, out Guid oturumAnahtari))
        {
            await oturumService.KapatAsync(oturumAnahtari, "Kullanıcı çıkış yaptı");
        }

        await logService.YazAsync(HttpContext, LogTurleri.Cikis, "Çıkış yapıldı",
            tenant.KullaniciId, tenant.FirmaId, User.Identity?.Name);

        return NoContent();
    }

    // Giriş yapan kullanıcının bilgilerini getirir
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var kullanici = await db.Kullanici
            .Include(x => x.Firma)
            .FirstOrDefaultAsync(x => x.KullaniciId == tenant.KullaniciId);

        if (kullanici == null)
        {
            return NotFound(new { mesaj = "Kullanıcı bulunamadı." });
        }

        return Ok(new
        {
            kullanici.KullaniciId,
            kullanici.KulAdi,
            kullanici.AdSoyad,
            kullanici.Email,
            kullanici.Rol,
            kullanici.FirmaId,
            FirmaKodu = kullanici.Firma!.FirmaKodu,
            FirmaAdi = kullanici.Firma.Adi
        });
    }
}
