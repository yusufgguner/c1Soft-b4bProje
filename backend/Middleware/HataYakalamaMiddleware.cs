using System.Net;
using c1Soft_b4bProje.Services;

namespace c1Soft_b4bProje.Middleware;

public class HataYakalamaMiddleware
{
    private readonly RequestDelegate next;

    public HataYakalamaMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    // Yakalanmayan hatayı loglar, kullanıcıya iç detay göstermeden hata numarası döner
    public async Task InvokeAsync(HttpContext context, LogService logService, ILogger<HataYakalamaMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (Exception hata) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(hata, "Beklenmeyen hata: {Yol}", context.Request.Path);

            int? hataNo = await logService.HataYazAsync(context, hata);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            string mesaj = hataNo.HasValue
                ? $"Beklenmeyen bir hata oluştu. Hata no: {hataNo}"
                : "Beklenmeyen bir hata oluştu.";

            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(
                    "<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>Hata</title>" +
                    "<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/@tabler/core@1.4.0/dist/css/tabler.min.css\"></head>" +
                    "<body><div class=\"page page-center\"><div class=\"container container-tight py-4 text-center\">" +
                    $"<h1 class=\"mb-3\">Bir şeyler ters gitti</h1><p class=\"text-secondary\">{WebUtility.HtmlEncode(mesaj)}</p>" +
                    "<a href=\"/admin\" class=\"btn btn-primary\">Panele dön</a></div></div></body></html>");
                return;
            }

            await context.Response.WriteAsJsonAsync(new { mesaj, hataNo });
        }
    }
}
