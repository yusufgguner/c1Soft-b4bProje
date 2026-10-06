using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/siparisler")]
[Authorize]
public class SiparislerController : ControllerBase
{
    private static readonly string[] Durumlar = { "Pending", "Approved", "Rejected", "Shipped", "Delivered", "Cancelled" };

    private readonly ApplicationDbContext db;
    private readonly TenantService tenant;

    public SiparislerController(ApplicationDbContext db, TenantService tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    // Siparişleri listeler
    [HttpGet]
    public async Task<List<SiparisDto>> GetAll()
    {
        var siparisler = await SiparisSorgusu()
            .OrderByDescending(x => x.Tarih)
            .ToListAsync();

        return siparisler.Select(DtoyaCevir).ToList();
    }

    // Siparişi id ile getirir
    [HttpGet("{id}")]
    public async Task<ActionResult<SiparisDto>> Get(int id)
    {
        var siparis = await SiparisSorgusu().FirstOrDefaultAsync(x => x.SiparisId == id);

        if (siparis == null)
        {
            return NotFound(new { mesaj = "Sipariş bulunamadı." });
        }

        return DtoyaCevir(siparis);
    }

    // Yeni sipariş oluşturur ve stoktan düşer
    [HttpPost]
    public async Task<IActionResult> Create(SiparisOlusturRequest model)
    {
        var istenenler = model.Kalemler
            .GroupBy(x => x.UrunId)
            .Select(x => new { UrunId = x.Key, Miktar = x.Sum(k => k.Miktar) })
            .ToList();

        var urunIdler = istenenler.Select(x => x.UrunId).ToList();

        var urunler = await db.Products
            .Where(x => urunIdler.Contains(x.ProductId) && x.IsActive)
            .ToListAsync();

        var siparis = new SiparisR
        {
            SiparisNo = "SIP-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
            FirmaId = tenant.FirmaId!.Value,
            KullaniciId = tenant.KullaniciId,
            Tarih = DateTime.Now,
            Notu = model.Notu,
            TeslimatAdresi = model.TeslimatAdresi,
            SiparisDurumu = "Pending"
        };

        foreach (var istenen in istenenler)
        {
            var urun = urunler.FirstOrDefault(x => x.ProductId == istenen.UrunId);

            if (urun == null)
            {
                return BadRequest(new { mesaj = $"{istenen.UrunId} numaralı ürün bulunamadı." });
            }

            if (istenen.Miktar > urun.StockQuantity)
            {
                return BadRequest(new { mesaj = $"{urun.ProductName} için yeterli stok yok. Mevcut stok: {urun.StockQuantity}" });
            }

            var kalem = new SiparisD
            {
                UrunId = urun.ProductId,
                UrunKodu = urun.ProductCode,
                UrunAdi = urun.ProductName,
                Miktar = istenen.Miktar,
                BirimFiyat = urun.Price
            };

            SiparisHesaplama.KalemHesapla(kalem);
            siparis.Kalemler.Add(kalem);

            urun.StockQuantity -= istenen.Miktar;
        }

        SiparisHesaplama.ToplamlariHesapla(siparis);

        db.SiparisR.Add(siparis);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = siparis.SiparisId }, new { siparis.SiparisId, siparis.SiparisNo, siparis.GenelTutar });
    }

    // Sipariş durumunu günceller, iptal ve redde stoğu geri ekler
    [HttpPut("{id}/durum")]
    [Authorize(Roles = Roller.FirmaAdmin)]
    public async Task<IActionResult> DurumGuncelle(int id, SiparisDurumRequest model)
    {
        if (!Durumlar.Contains(model.SiparisDurumu))
        {
            return BadRequest(new { mesaj = "Geçersiz sipariş durumu. Geçerli değerler: " + string.Join(", ", Durumlar) });
        }

        var siparis = await db.SiparisR
            .Include(x => x.Kalemler)
            .FirstOrDefaultAsync(x => x.SiparisId == id);

        if (siparis == null)
        {
            return NotFound(new { mesaj = "Sipariş bulunamadı." });
        }

        if (siparis.SiparisDurumu == "Cancelled" || siparis.SiparisDurumu == "Rejected")
        {
            return BadRequest(new { mesaj = "İptal edilmiş ya da reddedilmiş sipariş güncellenemez." });
        }

        if (model.SiparisDurumu == "Cancelled" || model.SiparisDurumu == "Rejected")
        {
            var urunIdler = siparis.Kalemler.Select(x => x.UrunId).ToList();
            var urunler = await db.Products.Where(x => urunIdler.Contains(x.ProductId)).ToListAsync();

            foreach (var kalem in siparis.Kalemler)
            {
                var urun = urunler.FirstOrDefault(x => x.ProductId == kalem.UrunId);

                if (urun != null)
                {
                    urun.StockQuantity += kalem.Miktar;
                }
            }
        }

        siparis.SiparisDurumu = model.SiparisDurumu;
        siparis.GuncellemeTarihi = DateTime.Now;

        await db.SaveChangesAsync();

        return NoContent();
    }

    // Normal kullanıcı sadece kendi siparişlerini görsün diye sorguyu hazırlar
    private IQueryable<SiparisR> SiparisSorgusu()
    {
        var sorgu = db.SiparisR
            .Include(x => x.Kalemler)
            .Include(x => x.Kullanici)
            .AsQueryable();

        if (tenant.Rol == Roller.Kullanici)
        {
            sorgu = sorgu.Where(x => x.KullaniciId == tenant.KullaniciId);
        }

        return sorgu;
    }

    // Siparişi DTO'ya çevirir
    private static SiparisDto DtoyaCevir(SiparisR x)
    {
        return new SiparisDto
        {
            SiparisId = x.SiparisId,
            SiparisNo = x.SiparisNo,
            KullaniciId = x.KullaniciId,
            KulAdi = x.Kullanici?.KulAdi ?? "",
            Tarih = x.Tarih,
            BrutTutar = x.BrutTutar,
            VergiTutar = x.VergiTutar,
            GenelTutar = x.GenelTutar,
            Notu = x.Notu,
            TeslimatAdresi = x.TeslimatAdresi,
            SiparisDurumu = x.SiparisDurumu,
            Kalemler = x.Kalemler.Select(k => new SiparisKalemDto
            {
                UrunId = k.UrunId,
                UrunKodu = k.UrunKodu,
                UrunAdi = k.UrunAdi,
                Miktar = k.Miktar,
                BirimFiyat = k.BirimFiyat,
                BirimTutar = k.BirimTutar,
                KDVOrani = k.KDVOrani,
                KDVTutari = k.KDVTutari,
                GenelToplam = k.GenelToplam
            }).ToList()
        };
    }
}
