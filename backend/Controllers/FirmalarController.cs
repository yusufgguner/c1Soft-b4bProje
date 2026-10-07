using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/firmalar")]
[Authorize(Roles = Roller.SistemAdmin)]
public class FirmalarController : ControllerBase
{
    private readonly ApplicationDbContext db;

    public FirmalarController(ApplicationDbContext db)
    {
        this.db = db;
    }

    // Tüm firmaları listeler
    [HttpGet]
    public async Task<List<FirmaDto>> GetAll()
    {
        return await db.Firma
            .OrderBy(x => x.Adi)
            .Select(x => new FirmaDto
            {
                FirmaId = x.FirmaId,
                FirmaKodu = x.FirmaKodu,
                Adi = x.Adi,
                VergiNo = x.VergiNo,
                Telefon = x.Telefon,
                Email = x.Email,
                Sehir = x.Sehir,
                IsAktif = x.IsAktif,
                KullaniciSayisi = x.Kullanicilar.Count
            })
            .ToListAsync();
    }

    // Firmayı id ile getirir
    [HttpGet("{id}")]
    public async Task<ActionResult<FirmaDto>> Get(int id)
    {
        var firma = await db.Firma
            .Where(x => x.FirmaId == id)
            .Select(x => new FirmaDto
            {
                FirmaId = x.FirmaId,
                FirmaKodu = x.FirmaKodu,
                Adi = x.Adi,
                VergiNo = x.VergiNo,
                Telefon = x.Telefon,
                Email = x.Email,
                Sehir = x.Sehir,
                IsAktif = x.IsAktif,
                KullaniciSayisi = x.Kullanicilar.Count
            })
            .FirstOrDefaultAsync();

        if (firma == null)
        {
            return NotFound(new { mesaj = "Firma bulunamadı." });
        }

        return firma;
    }

    // Yeni firma ekler
    [HttpPost]
    public async Task<IActionResult> Create(FirmaKaydetRequest model)
    {
        string firmaKodu = model.FirmaKodu.Trim().ToUpper();

        if (await db.Firma.AnyAsync(x => x.FirmaKodu == firmaKodu))
        {
            return Conflict(new { mesaj = "Bu firma kodu zaten kullanılıyor." });
        }

        var firma = new Firma
        {
            FirmaKodu = firmaKodu,
            Adi = model.Adi,
            VergiNo = model.VergiNo,
            Telefon = model.Telefon,
            Email = model.Email,
            Sehir = model.Sehir,
            IsAktif = model.IsAktif
        };

        db.Firma.Add(firma);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = firma.FirmaId }, new { firma.FirmaId, firma.FirmaKodu });
    }

    // Firma bilgilerini günceller
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, FirmaKaydetRequest model)
    {
        var firma = await db.Firma.FindAsync(id);

        if (firma == null)
        {
            return NotFound(new { mesaj = "Firma bulunamadı." });
        }

        string firmaKodu = model.FirmaKodu.Trim().ToUpper();

        if (await db.Firma.AnyAsync(x => x.FirmaKodu == firmaKodu && x.FirmaId != id))
        {
            return Conflict(new { mesaj = "Bu firma kodu zaten kullanılıyor." });
        }

        firma.FirmaKodu = firmaKodu;
        firma.Adi = model.Adi;
        firma.VergiNo = model.VergiNo;
        firma.Telefon = model.Telefon;
        firma.Email = model.Email;
        firma.Sehir = model.Sehir;
        firma.IsAktif = model.IsAktif;

        await db.SaveChangesAsync();

        return NoContent();
    }
}
