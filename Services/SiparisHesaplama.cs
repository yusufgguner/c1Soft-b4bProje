using c1Soft_b4bProje.Models;

namespace c1Soft_b4bProje.Services;

public static class SiparisHesaplama
{
    public const decimal KdvOrani = 20;

    // Kalemin tutarını, KDV'sini ve toplamını hesaplar
    public static void KalemHesapla(SiparisD kalem)
    {
        kalem.KDVOrani = KdvOrani;
        kalem.BirimTutar = kalem.BirimFiyat * kalem.Miktar;
        kalem.KDVTutari = Math.Round(kalem.BirimTutar * kalem.KDVOrani / 100, 2);
        kalem.GenelToplam = kalem.BirimTutar + kalem.KDVTutari;
    }

    // Siparişin genel toplamlarını kalemlerden hesaplar
    public static void ToplamlariHesapla(SiparisR siparis)
    {
        decimal brutTutar = 0;
        decimal vergiTutar = 0;
        decimal genelTutar = 0;

        foreach (var kalem in siparis.Kalemler)
        {
            brutTutar += kalem.BirimTutar;
            vergiTutar += kalem.KDVTutari;
            genelTutar += kalem.GenelToplam;
        }

        siparis.BrutTutar = brutTutar;
        siparis.VergiTutar = vergiTutar;
        siparis.GenelTutar = genelTutar;
        siparis.GuncellemeTarihi = DateTime.Now;
    }
}
