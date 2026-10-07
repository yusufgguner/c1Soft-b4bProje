-- arama testi icin ornek otomotiv urunleri (DEMO1 ve DEMO2)
-- tekrar calistirilirsa ayni kodlari eklemez
USE C1SoftB4B;
GO

SET NOCOUNT ON;

DECLARE @firmalar TABLE (FirmaId INT);
INSERT INTO @firmalar SELECT FirmaId FROM Firma WHERE FirmaKodu IN ('DEMO1', 'DEMO2');

WITH parca AS (
    SELECT * FROM (VALUES
        ('FRN', N'Ön Fren Balatası'), ('FRA', N'Arka Fren Balatası'), ('DSK', N'Ön Fren Diski'), ('DSA', N'Arka Fren Diski'),
        ('YGF', N'Yağ Filtresi'), ('HVF', N'Hava Filtresi'), ('PLF', N'Polen Filtresi'), ('YKF', N'Yakıt Filtresi'),
        ('AMO', N'Ön Amortisör'), ('AMA', N'Arka Amortisör'), ('BUJ', N'Buji Takımı'), ('ATB', N'Ateşleme Bobini'),
        ('TRG', N'Triger Seti'), ('DVR', N'Devirdaim Pompası'), ('RAD', N'Radyatör'), ('TRM', N'Termostat'),
        ('DBR', N'Debriyaj Seti'), ('VLN', N'V Kayışı'), ('ROT', N'Rot Başı'), ('SLC', N'Salıncak'),
        ('RLM', N'Ön Teker Rulmanı'), ('SIL', N'Silecek Süpürgesi'), ('FAR', N'Far Ampulü'), ('AKU', N'Akü'),
        ('KLM', N'Klima Kompresörü'), ('EGZ', N'Egzoz Susturucu'), ('MRS', N'Marş Motoru'), ('ALT', N'Alternatör'),
        ('ENJ', N'Enjektör'), ('SNS', N'Oksijen Sensörü')
    ) p(Onek, Ad)
),
arac AS (
    SELECT * FROM (VALUES
        (N'Fiat Egea'), (N'Renault Clio'), (N'Renault Megane'), (N'Volkswagen Golf'), (N'Volkswagen Passat'),
        (N'Ford Focus'), (N'Ford Fiesta'), (N'Toyota Corolla'), (N'Hyundai i20'), (N'Hyundai Accent'),
        (N'Opel Astra'), (N'Opel Corsa'), (N'Peugeot 301'), (N'Peugeot 3008'), (N'Citroen C-Elysee'),
        (N'Dacia Duster'), (N'Honda Civic'), (N'Skoda Octavia'), (N'Seat Leon'), (N'Tofaş Şahin'),
        (N'BMW 3 Serisi'), (N'Mercedes C Serisi'), (N'Audi A3'), (N'Nissan Qashqai'), (N'Kia Sportage')
    ) a(Arac)
),
marka AS (
    SELECT * FROM (VALUES
        (N'Bosch'), (N'Brembo'), (N'Valeo'), (N'Mann'), (N'Mahle'), (N'Sachs'), (N'NGK'), (N'Gates'),
        (N'Febi'), (N'SKF'), (N'TRW'), (N'Varta'), (N'Denso'), (N'Lemförder'), (N'Hella'), (N'Continental'),
        (N'Delphi'), (N'Monroe'), (N'LuK'), (N'Nissens')
    ) m(Marka)
),
liste AS (
    SELECT f.FirmaId, p.Onek, p.Ad + N' ' + a.Arac AS Ad, m.Marka,
           ROW_NUMBER() OVER (PARTITION BY f.FirmaId ORDER BY p.Onek, a.Arac, m.Marka) AS No
    FROM @firmalar f
    CROSS JOIN parca p
    CROSS JOIN arac a
    CROSS JOIN marka m
)
INSERT INTO Products (FirmaId, ProductCode, ProductName, Brand, Price, StockQuantity, IsActive, CreatedAt)
SELECT l.FirmaId,
       l.Onek + '-' + RIGHT('00000' + CAST(l.No AS VARCHAR(10)), 5),
       l.Ad,
       l.Marka,
       CAST(100 + ABS(CHECKSUM(NEWID())) % 9900 AS DECIMAL(18, 2)),
       ABS(CHECKSUM(NEWID())) % 200,
       1,
       GETDATE()
FROM liste l
WHERE NOT EXISTS (
    SELECT 1 FROM Products x
    WHERE x.FirmaId = l.FirmaId AND x.ProductCode = l.Onek + '-' + RIGHT('00000' + CAST(l.No AS VARCHAR(10)), 5)
);

SELECT FirmaId, COUNT(*) AS UrunSayisi FROM Products GROUP BY FirmaId;
GO
