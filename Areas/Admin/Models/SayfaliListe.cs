using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Areas.Admin.Models;

public class SayfaliListe<T>
{
    public List<T> Kayitlar { get; set; } = new List<T>();
    public int Sayfa { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamKayit { get; set; }

    public int ToplamSayfa => Math.Max(1, (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu));

    // Sorgunun istenen sayfasını ve toplam kayıt sayısını getirir
    public static async Task<SayfaliListe<T>> OlusturAsync(IQueryable<T> sorgu, int sayfa, int sayfaBoyutu = 50)
    {
        if (sayfa < 1)
        {
            sayfa = 1;
        }

        return new SayfaliListe<T>
        {
            ToplamKayit = await sorgu.CountAsync(),
            Kayitlar = await sorgu.Skip((sayfa - 1) * sayfaBoyutu).Take(sayfaBoyutu).ToListAsync(),
            Sayfa = sayfa,
            SayfaBoyutu = sayfaBoyutu
        };
    }
}
