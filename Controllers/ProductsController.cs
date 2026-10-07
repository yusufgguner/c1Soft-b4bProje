using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Dtos;
using c1Soft_b4bProje.Models;
using System.Globalization;
using c1Soft_b4bProje.Services;
using c1Soft_b4bProje.Services.Arama;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext db;
    private readonly TenantService tenant;

    public ProductsController(ApplicationDbContext db, TenantService tenant)
    {
        this.db = db;
        this.tenant = tenant;
    }

    // Ürünleri arama ve aktiflik filtresiyle listeler
    [HttpGet]
    public async Task<List<ProductDto>> GetAll(string? arama, bool sadeceAktif = true)
    {
        var sorgu = db.Products.AsQueryable();

        if (sadeceAktif)
        {
            sorgu = sorgu.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            sorgu = sorgu.Where(x =>
                x.ProductCode.Contains(arama) ||
                x.ProductName.Contains(arama) ||
                (x.Brand != null && x.Brand.Contains(arama)));
        }

        return await sorgu
            .OrderBy(x => x.ProductName)
            .Select(x => new ProductDto
            {
                ProductId = x.ProductId,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                Brand = x.Brand,
                Price = x.Price,
                StockQuantity = x.StockQuantity,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    // Ürünleri arama motoruyla arar, süreyi ve kaynağı başlıkta döner
    [HttpGet("search")]
    public async Task<ActionResult<UrunAramaSonucu>> Search([FromQuery] UrunAramaIstegi istek, [FromServices] ProductSearchService arama)
    {
        if (tenant.FirmaId == null)
        {
            return Forbid();
        }

        var (sonuc, kaynak, ms) = await arama.AraAsync(tenant.FirmaId.Value, istek);
        Response.Headers["X-Search-Ms"] = ms.ToString("0.000", CultureInfo.InvariantCulture);
        Response.Headers["X-Search-Source"] = kaynak;
        return sonuc;
    }

    // Ürünü id ile getirir
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        var urun = await db.Products.FirstOrDefaultAsync(x => x.ProductId == id);

        if (urun == null)
        {
            return NotFound(new { mesaj = "Ürün bulunamadı." });
        }

        return DtoyaCevir(urun);
    }

    // Yeni ürün ekler
    [HttpPost]
    [Authorize(Roles = Roller.FirmaAdmin)]
    public async Task<IActionResult> Create(ProductKaydetRequest model)
    {
        if (await db.Products.AnyAsync(x => x.ProductCode == model.ProductCode))
        {
            return Conflict(new { mesaj = "Bu ürün kodu firmanızda zaten var." });
        }

        var urun = new Product
        {
            FirmaId = tenant.FirmaId!.Value,
            ProductCode = model.ProductCode,
            ProductName = model.ProductName,
            Brand = model.Brand,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            IsActive = model.IsActive
        };

        db.Products.Add(urun);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = urun.ProductId }, DtoyaCevir(urun));
    }

    // Ürünü günceller
    [HttpPut("{id}")]
    [Authorize(Roles = Roller.FirmaAdmin)]
    public async Task<IActionResult> Update(int id, ProductKaydetRequest model)
    {
        var urun = await db.Products.FirstOrDefaultAsync(x => x.ProductId == id);

        if (urun == null)
        {
            return NotFound(new { mesaj = "Ürün bulunamadı." });
        }

        if (await db.Products.AnyAsync(x => x.ProductCode == model.ProductCode && x.ProductId != id))
        {
            return Conflict(new { mesaj = "Bu ürün kodu firmanızda zaten var." });
        }

        urun.ProductCode = model.ProductCode;
        urun.ProductName = model.ProductName;
        urun.Brand = model.Brand;
        urun.Price = model.Price;
        urun.StockQuantity = model.StockQuantity;
        urun.IsActive = model.IsActive;

        await db.SaveChangesAsync();

        return NoContent();
    }

    // Ürünü silmeden pasife alır
    [HttpDelete("{id}")]
    [Authorize(Roles = Roller.FirmaAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var urun = await db.Products.FirstOrDefaultAsync(x => x.ProductId == id);

        if (urun == null)
        {
            return NotFound(new { mesaj = "Ürün bulunamadı." });
        }

        urun.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Ürünü DTO'ya çevirir
    private static ProductDto DtoyaCevir(Product x)
    {
        return new ProductDto
        {
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            Brand = x.Brand,
            Price = x.Price,
            StockQuantity = x.StockQuantity,
            IsActive = x.IsActive
        };
    }
}
