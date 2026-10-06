using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/kullanicilar")]
[Authorize(Roles = Roller.FirmaAdmin + "," + Roller.SistemAdmin)]
public class KullanicilarController : ControllerBase
{
    private readonly ApplicationDbContext db;
    private readonly PasswordService passwordService;
    private readonly TenantService tenant;

    public KullanicilarController(ApplicationDbContext db, PasswordService passwordService, TenantService tenant)
    {
        this.db = db;
        this.passwordService = passwordService;
        this.tenant = tenant;
    }

    // Firmadaki kullanıcıları listeler
    [HttpGet]
    public async Task<List<KullaniciDto>> GetAll()
    {
        return await db.Kullanici
            .Where(x => x.FirmaId == tenant.FirmaId)
            .OrderBy(x => x.KulAdi)
            .Select(x => new KullaniciDto
            {
                KullaniciId = x.KullaniciId,
                FirmaId = x.FirmaId,
                KulAdi = x.KulAdi,
                AdSoyad = x.AdSoyad,
                Email = x.Email,
                Rol = x.Rol,
                IsAktif = x.IsAktif,
                SonGirisTarihi = x.SonGirisTarihi
            })
            .ToListAsync();
    }

    // Firmaya yeni kullanıcı ekler
    [HttpPost]
    public async Task<IActionResult> Create(KullaniciEkleRequest model)
    {
        if (!RolGecerliMi(model.Rol))
        {
            return BadRequest(new { mesaj = "Geçersiz rol." });
        }

        if (await db.Kullanici.AnyAsync(x => x.FirmaId == tenant.FirmaId && x.KulAdi == model.KulAdi))
        {
            return Conflict(new { mesaj = "Bu kullanıcı adı firmanızda zaten var." });
        }

        var kullanici = new Kullanici
        {
            FirmaId = tenant.FirmaId!.Value,
            KulAdi = model.KulAdi,
            SifreHash = passwordService.Hash(model.Sifre),
            AdSoyad = model.AdSoyad,
            Email = model.Email,
            Rol = model.Rol
        };

        db.Kullanici.Add(kullanici);
        await db.SaveChangesAsync();

        return Created($"/api/kullanicilar/{kullanici.KullaniciId}", new { kullanici.KullaniciId, kullanici.KulAdi });
    }

    // Kullanıcı bilgilerini günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, KullaniciGuncelleRequest model)
    {
        if (!RolGecerliMi(model.Rol))
        {
            return BadRequest(new { mesaj = "Geçersiz rol." });
        }

        var kullanici = await KullaniciGetir(id);

        if (kullanici == null)
        {
            return NotFound(new { mesaj = "Kullanıcı bulunamadı." });
        }

        kullanici.AdSoyad = model.AdSoyad;
        kullanici.Email = model.Email;
        kullanici.Rol = model.Rol;
        kullanici.IsAktif = model.IsAktif;

        if (!string.IsNullOrWhiteSpace(model.YeniSifre))
        {
            kullanici.SifreHash = passwordService.Hash(model.YeniSifre);
        }

        await db.SaveChangesAsync();

        return NoContent();
    }

    // Kullanıcıyı silmeden pasife alır
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == tenant.KullaniciId)
        {
            return BadRequest(new { mesaj = "Kendi hesabınızı pasife alamazsınız." });
        }

        var kullanici = await KullaniciGetir(id);

        if (kullanici == null)
        {
            return NotFound(new { mesaj = "Kullanıcı bulunamadı." });
        }

        kullanici.IsAktif = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Kullanıcıyı sadece kendi firmasında arar
    private async Task<Kullanici?> KullaniciGetir(int id)
    {
        return await db.Kullanici.FirstOrDefaultAsync(x => x.KullaniciId == id && x.FirmaId == tenant.FirmaId);
    }

    // Verilen rolün atanabilir olup olmadığını kontrol eder
    private bool RolGecerliMi(string rol)
    {
        if (rol == Roller.SistemAdmin)
        {
            return tenant.Rol == Roller.SistemAdmin;
        }

        return Roller.Hepsi.Contains(rol);
    }
}
