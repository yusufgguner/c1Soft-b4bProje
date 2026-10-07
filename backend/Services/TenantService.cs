using System.Security.Claims;

namespace c1Soft_b4bProje.Services;

public class TenantService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? FirmaId
    {
        get
        {
            string? deger = User?.FindFirst(TokenService.FirmaIdClaim)?.Value;
            return int.TryParse(deger, out int firmaId) ? firmaId : null;
        }
    }

    public int KullaniciId
    {
        get
        {
            string? deger = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(deger, out int kullaniciId) ? kullaniciId : 0;
        }
    }

    public string Rol => User?.FindFirst(ClaimTypes.Role)?.Value ?? "";
}
