using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using c1Soft_b4bProje.Models;
using Microsoft.IdentityModel.Tokens;

namespace c1Soft_b4bProje.Services;

public class TokenService
{
    public const string FirmaIdClaim = "FirmaId";
    public const string FirmaKoduClaim = "FirmaKodu";

    private readonly IConfiguration configuration;

    public TokenService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    // Kullanıcı ve firma bilgileriyle JWT token üretir
    public (string Token, DateTime BitisTarihi) TokenOlustur(Kullanici kullanici, Firma firma, Guid oturumAnahtari)
    {
        var jwt = configuration.GetSection("Jwt");
        int sureDakika = jwt.GetValue<int>("SureDakika");
        DateTime bitisTarihi = DateTime.UtcNow.AddMinutes(sureDakika);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KulAdi),
            new Claim(ClaimTypes.Role, kullanici.Rol),
            new Claim(FirmaIdClaim, firma.FirmaId.ToString()),
            new Claim(FirmaKoduClaim, firma.FirmaKodu),
            new Claim(OturumService.OturumClaim, oturumAnahtari.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: bitisTarihi,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), bitisTarihi);
    }
}
