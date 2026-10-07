# c1Soft B4B

- `backend/`: .NET 9 Web API (+ yönetim paneli `/admin`)
- `frontend/`: React + Vite + TypeScript kullanıcı ekranı (giriş, ürün listesi ve arama)

## Çalıştırma

1. `docker compose up -d` (Elasticsearch + Redis, aşağıdaki kurulum adımlarına bak)
2. Backend: `cd backend` → `dotnet run --launch-profile http` (http://localhost:5178)
3. Frontend: `cd frontend` → `npm install` → `npm run dev` (http://localhost:5173)

Frontend `/api` isteklerini geliştirmede backend'e yönlendiriyor. Canlıda frontend başka adresten yayınlanacaksa adresi `backend/appsettings.json` içindeki `Cors:Adresler` listesine ekle.

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
3. `backend` klasörüne `appsettings.Local.json` oluştur:

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

### Örnek ürünler ve hız testi

Arama denemek için `tools/ornek-urunler.sql` DEMO1 ve DEMO2'ye 15.000'er otomotiv ürünü ekler (fren, filtre, amortisör, akü vs. + araç modeli + marka). Ekledikten sonra indeksi yenile (`POST /api/admin/search/reindex`) ya da indeksi silip projeyi yeniden başlat.

Hız testi: `.\tools\arama-benchmark.ps1 -FirmaKodu DEMO1 -KulAdi admin -Sifre ****`

Benim makinemde 30.000 ürünle, 100 farklı arama:

| Durum | Kaynak | Ortalama | p95 | En kötü |
|---|---|---|---|---|
| Tekrar eden arama | memory | 0,01 ms | 0,01 ms | 0,62 ms |
| İlk kez yapılan arama | elastic | 11 ms | 17 ms | 38 ms |

Tekrar eden aramalar 1 ms altında. İlk kez yapılan arama Elasticsearch'e gidiyor, o yüzden birkaç ms sürüyor; sonuç önbelleğe yazıldıktan sonra aynı arama 1 ms altına iniyor.
