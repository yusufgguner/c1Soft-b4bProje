# c1Soft B4B

- `backend/`: .NET 9 Web API + yönetim paneli (`/admin`)
- `frontend/`: React kullanıcı ekranı (giriş, ürün arama)

## Kurulum

1. `.env.example` → `.env` kopyala, şifreleri yaz.
2. `docker compose up -d` (Elasticsearch + Redis)
3. `backend/appsettings.Local.json` oluştur:

```json
{
  "Arama": {
    "ElasticSifre": "ELASTIC_PASSWORD",
    "RedisBaglanti": "localhost:6379,password=REDIS_PASSWORD"
  }
}
```

4. Backend: `cd backend` → `dotnet run --launch-profile http`
5. Frontend: `cd frontend` → `npm install` → `npm run dev` → http://localhost:5173

## Ürün arama

Bellek → Redis → Elasticsearch sırasıyla çalışır, ES kapalıysa SQL'den arar. Ürün veya stok değişince indeks güncellenir.

`GET /api/products/search?q=balata&marka=Brembo&stoktaOlan=true&sayfa=1`

Süre `X-Search-Ms`, kaynak `X-Search-Source` başlığında döner.

Örnek veri: `tools/ornek-urunler.sql` (30.000 ürün), hız testi: `tools/arama-benchmark.ps1`

30.000 üründe tekrar eden arama ort. 0,01 ms, ilk arama ort. 11 ms.
