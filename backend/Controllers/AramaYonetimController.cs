using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services.Arama;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace c1Soft_b4bProje.Controllers;

[ApiController]
[Route("api/admin/search")]
[Authorize(Roles = Roller.SistemAdmin)]
public class AramaYonetimController : ControllerBase
{
    private readonly ProductIndexSync sync;

    public AramaYonetimController(ProductIndexSync sync)
    {
        this.sync = sync;
    }

    // Arama indeksini baştan kurar ve tüm ürünleri yükler
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        await sync.TumunuYukleAsync();
        return Ok(new { mesaj = "Arama indeksi yeniden yüklendi." });
    }
}
