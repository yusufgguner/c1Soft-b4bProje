# c1Soft B4B API

## Ürün arama (Elasticsearch + Redis)

Ürün araması üç katmanlı çalışıyor:

1. Uygulama belleği: aynı arama yakın zamanda yapıldıysa sonuç buradan gelir.
2. Redis: arama başka bir istekte yapıldıysa sonuç buradan gelir.
3. Elasticsearch: ilk kez yapılan aramalar buraya gider, sonuç Redis'e ve belleğe yazılır.

Elasticsearch ya da Redis kapalıysa arama SQL üzerinden devam eder.

Ürün eklenince, güncellenince ya da sipariş stoğu değiştirince indeks güncellenir ve o firmanın önbelleği geçersiz olur.

### Kurulum

Docker Desktop açık olmalı.

1. `.env.example` dosyasını `.env` olarak kopyalayıp iki şifreyi yaz.
2. `docker compose up -d`
3. Proje klasörüne `appsettings.Local.json` oluştur:

```json
{
  "Arama": {
    "ElasticSifre": ".env'deki ELASTIC_PASSWORD",
    "RedisBaglanti": "localhost:6379,password=.env'deki REDIS_PASSWORD"
  }
}
```

4. Projeyi çalıştır. İndeks yoksa açılışta kurulur ve ürünler yüklenir.

`.env` ve `appsettings.Local.json` git'e girmez.

### Kullanım

`GET /api/products/search?q=balata&marka=Brembo&minFiyat=100&maxFiyat=900&stoktaOlan=true&sayfa=1`

- `q`: ürün kodu, adı veya markası. Kod tam eşleşirse en üstte gelir, kodun başı da yeterli (`FRN` → `FRN-010`). Türkçe karakter ve küçük yazım hataları tolere edilir (`balatasi`, `blata`).
- Her firma sadece kendi ürünlerini görür.

Yanıt başlıkları:

- `X-Search-Ms`: arama süresi (ms, sunucu içinde ölçülür)
- `X-Search-Source`: sonucun geldiği yer (`memory`, `redis`, `elastic`, `sql`)

Sistem yöneticisi indeksi baştan kurmak için: `POST /api/admin/search/reindex`
