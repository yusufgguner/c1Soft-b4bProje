using System.Diagnostics;
using System.Security.Claims;
using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;

namespace c1Soft_b4bProje.Middleware;

public class IslemLogMiddleware
{
    private static readonly string[] AtlanacakYollar = { "/api/auth/login" };

    private readonly RequestDelegate next;

    public IslemLogMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    // Her api isteğini kim, ne zaman, ne yaptı ve sonucu ne oldu diye loglar
    public async Task InvokeAsync(HttpContext context, LogService logService, ILogger<IslemLogMiddleware> logger)
    {
        string yol = context.Request.Path.Value ?? "";

        if (!yol.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            AtlanacakYollar.Contains(yol, StringComparer.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var sure = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            sure.Stop();

            try
            {
                await logService.YazAsync(new IslemLog
                {
                    Tarih = DateTime.Now,
                    Tur = LogTurleri.Istek,
                    KullaniciId = SayiOku(context.User, ClaimTypes.NameIdentifier),
                    FirmaId = SayiOku(context.User, TokenService.FirmaIdClaim),
                    KulAdi = context.User.Identity?.Name,
                    Metot = context.Request.Method,
                    Yol = yol + context.Request.QueryString,
                    DurumKodu = context.Response.StatusCode,
                    Domain = OturumService.DomainBul(context),
                    IpAdresi = context.Connection.RemoteIpAddress?.ToString(),
                    SureMs = (int)sure.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "İşlem logu yazılamadı");
            }
        }
    }

    // Claim değerini sayıya çevirir, yoksa null döner
    private static int? SayiOku(ClaimsPrincipal user, string claim)
    {
        return int.TryParse(user.FindFirst(claim)?.Value, out int deger) ? deger : null;
    }
}
