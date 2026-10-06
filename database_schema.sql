IF DB_ID('C1SoftB4B') IS NULL
BEGIN
    CREATE DATABASE C1SoftB4B;
END
GO

USE C1SoftB4B;
GO

IF OBJECT_ID('Firma', 'U') IS NULL
BEGIN
    CREATE TABLE Firma
    (
        FirmaId INT IDENTITY(1,1) PRIMARY KEY,
        FirmaKodu NVARCHAR(20) NOT NULL,
        Adi NVARCHAR(150) NOT NULL,
        VergiNo NVARCHAR(20) NULL,
        Telefon NVARCHAR(30) NULL,
        Email NVARCHAR(150) NULL,
        Sehir NVARCHAR(50) NULL,
        IsAktif BIT NOT NULL DEFAULT 1,
        OlusturmaTarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_Firma_FirmaKodu UNIQUE (FirmaKodu)
    );
END
GO

IF OBJECT_ID('Kullanici', 'U') IS NULL
BEGIN
    CREATE TABLE Kullanici
    (
        KullaniciId INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId INT NOT NULL,
        KulAdi NVARCHAR(50) NOT NULL,
        SifreHash NVARCHAR(200) NOT NULL,
        AdSoyad NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NULL,
        Rol NVARCHAR(20) NOT NULL DEFAULT 'Kullanici',
        IsAktif BIT NOT NULL DEFAULT 1,
        SonGirisTarihi DATETIME2 NULL,
        OlusturmaTarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Kullanici_Firma FOREIGN KEY (FirmaId) REFERENCES Firma(FirmaId),
        CONSTRAINT UQ_Kullanici_Firma_KulAdi UNIQUE (FirmaId, KulAdi),
        CONSTRAINT CK_Kullanici_Rol CHECK (Rol IN ('SistemAdmin', 'FirmaAdmin', 'Kullanici'))
    );
END
GO

IF OBJECT_ID('Products', 'U') IS NULL
BEGIN
    CREATE TABLE Products
    (
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId INT NOT NULL,
        ProductCode NVARCHAR(50) NOT NULL,
        ProductName NVARCHAR(150) NOT NULL,
        Brand NVARCHAR(100) NULL,
        Price DECIMAL(18,2) NOT NULL DEFAULT 0,
        StockQuantity INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Products_Firma FOREIGN KEY (FirmaId) REFERENCES Firma(FirmaId),
        CONSTRAINT UQ_Products_Firma_ProductCode UNIQUE (FirmaId, ProductCode),
        CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0)
    );
END
GO

IF OBJECT_ID('SiparisR', 'U') IS NULL
BEGIN
    CREATE TABLE SiparisR
    (
        SiparisId INT IDENTITY(1,1) PRIMARY KEY,
        SiparisNo NVARCHAR(30) NOT NULL,
        FirmaId INT NOT NULL,
        KullaniciId INT NOT NULL,
        Tarih DATETIME2 NOT NULL DEFAULT GETDATE(),
        GuncellemeTarihi DATETIME2 NULL,
        BrutTutar DECIMAL(18,2) NOT NULL DEFAULT 0,
        VergiTutar DECIMAL(18,2) NOT NULL DEFAULT 0,
        GenelTutar DECIMAL(18,2) NOT NULL DEFAULT 0,
        Notu NVARCHAR(500) NULL,
        TeslimatAdresi NVARCHAR(300) NULL,
        SiparisDurumu NVARCHAR(30) NOT NULL DEFAULT 'Pending',
        CONSTRAINT FK_SiparisR_Firma FOREIGN KEY (FirmaId) REFERENCES Firma(FirmaId),
        CONSTRAINT FK_SiparisR_Kullanici FOREIGN KEY (KullaniciId) REFERENCES Kullanici(KullaniciId)
    );
END
GO

IF OBJECT_ID('SiparisD', 'U') IS NULL
BEGIN
    CREATE TABLE SiparisD
    (
        Sayac INT IDENTITY(1,1) PRIMARY KEY,
        SiparisId INT NOT NULL,
        UrunId INT NOT NULL,
        UrunKodu NVARCHAR(50) NOT NULL,
        UrunAdi NVARCHAR(150) NOT NULL,
        Miktar INT NOT NULL,
        BirimFiyat DECIMAL(18,2) NOT NULL,
        BirimTutar DECIMAL(18,2) NOT NULL,
        KDVOrani DECIMAL(5,2) NOT NULL,
        KDVTutari DECIMAL(18,2) NOT NULL,
        GenelToplam DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_SiparisD_SiparisR FOREIGN KEY (SiparisId) REFERENCES SiparisR(SiparisId),
        CONSTRAINT FK_SiparisD_Products FOREIGN KEY (UrunId) REFERENCES Products(ProductId),
        CONSTRAINT CK_SiparisD_Miktar CHECK (Miktar > 0)
    );
END
GO

IF OBJECT_ID('KullaniciOturum', 'U') IS NULL
BEGIN
    CREATE TABLE KullaniciOturum
    (
        OturumId INT IDENTITY(1,1) PRIMARY KEY,
        KullaniciId INT NOT NULL,
        FirmaId INT NOT NULL,
        OturumAnahtari UNIQUEIDENTIFIER NOT NULL,
        Domain NVARCHAR(200) NULL,
        IpAdresi NVARCHAR(50) NULL,
        Tarayici NVARCHAR(300) NULL,
        GirisTarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
        SonIslemTarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
        CikisTarihi DATETIME2 NULL,
        IsAktif BIT NOT NULL DEFAULT 1,
        KapanmaNedeni NVARCHAR(300) NULL,
        CONSTRAINT UQ_KullaniciOturum_Anahtar UNIQUE (OturumAnahtari),
        CONSTRAINT FK_KullaniciOturum_Kullanici FOREIGN KEY (KullaniciId) REFERENCES Kullanici(KullaniciId),
        CONSTRAINT FK_KullaniciOturum_Firma FOREIGN KEY (FirmaId) REFERENCES Firma(FirmaId)
    );

    CREATE INDEX IX_KullaniciOturum_Kullanici_Aktif ON KullaniciOturum (KullaniciId, IsAktif);
END
GO

IF OBJECT_ID('IslemLog', 'U') IS NULL
BEGIN
    CREATE TABLE IslemLog
    (
        LogId BIGINT IDENTITY(1,1) PRIMARY KEY,
        Tarih DATETIME2 NOT NULL DEFAULT GETDATE(),
        KullaniciId INT NULL,
        FirmaId INT NULL,
        KulAdi NVARCHAR(50) NULL,
        Tur NVARCHAR(30) NOT NULL,
        Metot NVARCHAR(10) NULL,
        Yol NVARCHAR(300) NULL,
        DurumKodu INT NULL,
        Domain NVARCHAR(200) NULL,
        IpAdresi NVARCHAR(50) NULL,
        SureMs INT NULL,
        Aciklama NVARCHAR(500) NULL
    );

    CREATE INDEX IX_IslemLog_Tarih ON IslemLog (Tarih DESC);
    CREATE INDEX IX_IslemLog_Kullanici ON IslemLog (KullaniciId);
END
GO

IF NOT EXISTS (SELECT 1 FROM Firma WHERE FirmaKodu = 'C1SOFT')
BEGIN
    INSERT INTO Firma (FirmaKodu, Adi, Sehir, Email)
    VALUES ('C1SOFT', 'C1Soft Yazılım', 'İstanbul', 'info@c1soft.local');
END

IF NOT EXISTS (SELECT 1 FROM Firma WHERE FirmaKodu = 'DEMO1')
BEGIN
    INSERT INTO Firma (FirmaKodu, Adi, VergiNo, Telefon, Sehir, Email)
    VALUES ('DEMO1', 'Demo Otomotiv', '1111111111', '0212 000 00 01', 'İstanbul', 'info@demo1.local');
END

IF NOT EXISTS (SELECT 1 FROM Firma WHERE FirmaKodu = 'DEMO2')
BEGIN
    INSERT INTO Firma (FirmaKodu, Adi, VergiNo, Telefon, Sehir, Email)
    VALUES ('DEMO2', 'Örnek Yedek Parça', '2222222222', '0264 000 00 02', 'Sakarya', 'info@demo2.local');
END
GO

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'C1SOFT' AND k.KulAdi = 'admin')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'admin', '3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121',
           'Sistem Yöneticisi', 'admin@c1soft.local', 'SistemAdmin'
    FROM Firma WHERE FirmaKodu = 'C1SOFT';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO1' AND k.KulAdi = 'admin')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'admin', '3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121',
           'Demo1 Yönetici', 'admin@demo1.local', 'FirmaAdmin'
    FROM Firma WHERE FirmaKodu = 'DEMO1';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO1' AND k.KulAdi = 'kullanici')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'kullanici', 'B86ED0FB118F2AC7926350E9FB9A297CE980BCFAA764908D46C3ED2848860695',
           'Demo1 Kullanıcı', 'kullanici@demo1.local', 'Kullanici'
    FROM Firma WHERE FirmaKodu = 'DEMO1';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO2' AND k.KulAdi = 'admin')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'admin', '3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121',
           'Demo2 Yönetici', 'admin@demo2.local', 'FirmaAdmin'
    FROM Firma WHERE FirmaKodu = 'DEMO2';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO2' AND k.KulAdi = 'kullanici')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'kullanici', 'B86ED0FB118F2AC7926350E9FB9A297CE980BCFAA764908D46C3ED2848860695',
           'Demo2 Kullanıcı', 'kullanici@demo2.local', 'Kullanici'
    FROM Firma WHERE FirmaKodu = 'DEMO2';
END
GO

IF NOT EXISTS (SELECT 1 FROM Products p JOIN Firma f ON f.FirmaId = p.FirmaId WHERE f.FirmaKodu = 'DEMO1')
BEGIN
    INSERT INTO Products (FirmaId, ProductCode, ProductName, Brand, Price, StockQuantity)
    SELECT f.FirmaId, v.ProductCode, v.ProductName, v.Brand, v.Price, v.StockQuantity
    FROM Firma f
    CROSS JOIN (VALUES
        ('FLT-001', 'Yağ Filtresi', 'Bosch', 185.00, 120),
        ('FRN-010', 'Ön Fren Balatası', 'Brembo', 1450.00, 40),
        ('AKU-060', 'Akü 60 Ah', 'Varta', 3250.00, 15),
        ('SIL-020', 'Silecek Süpürgesi', 'Valeo', 340.00, 75)
    ) v (ProductCode, ProductName, Brand, Price, StockQuantity)
    WHERE f.FirmaKodu = 'DEMO1';
END

IF NOT EXISTS (SELECT 1 FROM Products p JOIN Firma f ON f.FirmaId = p.FirmaId WHERE f.FirmaKodu = 'DEMO2')
BEGIN
    INSERT INTO Products (FirmaId, ProductCode, ProductName, Brand, Price, StockQuantity)
    SELECT f.FirmaId, v.ProductCode, v.ProductName, v.Brand, v.Price, v.StockQuantity
    FROM Firma f
    CROSS JOIN (VALUES
        ('FLT-001', 'Hava Filtresi', 'Mann', 210.00, 90),
        ('BUJ-004', 'Buji Takımı (4 lü)', 'NGK', 760.00, 60),
        ('AMR-110', 'Arka Amortisör', 'Sachs', 2100.00, 20)
    ) v (ProductCode, ProductName, Brand, Price, StockQuantity)
    WHERE f.FirmaKodu = 'DEMO2';
END
GO

IF NOT EXISTS (SELECT 1 FROM Firma WHERE FirmaKodu = 'DEMO3')
BEGIN
    INSERT INTO Firma (FirmaKodu, Adi, VergiNo, Telefon, Sehir, Email)
    VALUES ('DEMO3', 'Yıldız Oto Yedek', '3333333333', '0232 000 00 03', 'İzmir', 'info@demo3.local');
END
GO

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO1' AND k.KulAdi = 'ahmet')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'ahmet', 'B86ED0FB118F2AC7926350E9FB9A297CE980BCFAA764908D46C3ED2848860695',
           'Ahmet Yılmaz', 'ahmet@demo1.local', 'Kullanici'
    FROM Firma WHERE FirmaKodu = 'DEMO1';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO2' AND k.KulAdi = 'mehmet')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'mehmet', 'B86ED0FB118F2AC7926350E9FB9A297CE980BCFAA764908D46C3ED2848860695',
           'Mehmet Kaya', 'mehmet@demo2.local', 'Kullanici'
    FROM Firma WHERE FirmaKodu = 'DEMO2';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO3' AND k.KulAdi = 'admin')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'admin', '3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121',
           'Demo3 Yönetici', 'admin@demo3.local', 'FirmaAdmin'
    FROM Firma WHERE FirmaKodu = 'DEMO3';
END

IF NOT EXISTS (SELECT 1 FROM Kullanici k JOIN Firma f ON f.FirmaId = k.FirmaId WHERE f.FirmaKodu = 'DEMO3' AND k.KulAdi = 'kullanici')
BEGIN
    INSERT INTO Kullanici (FirmaId, KulAdi, SifreHash, AdSoyad, Email, Rol)
    SELECT FirmaId, 'kullanici', 'B86ED0FB118F2AC7926350E9FB9A297CE980BCFAA764908D46C3ED2848860695',
           'Demo3 Kullanıcı', 'kullanici@demo3.local', 'Kullanici'
    FROM Firma WHERE FirmaKodu = 'DEMO3';
END
GO

IF NOT EXISTS (SELECT 1 FROM Products p JOIN Firma f ON f.FirmaId = p.FirmaId WHERE f.FirmaKodu = 'DEMO3')
BEGIN
    INSERT INTO Products (FirmaId, ProductCode, ProductName, Brand, Price, StockQuantity)
    SELECT f.FirmaId, v.ProductCode, v.ProductName, v.Brand, v.Price, v.StockQuantity
    FROM Firma f
    CROSS JOIN (VALUES
        ('TRG-200', 'Triger Seti', 'Gates', 2850.00, 25),
        ('RAD-050', 'Radyatör', 'Nissens', 4100.00, 10),
        ('FLT-001', 'Polen Filtresi', 'Mahle', 260.00, 80)
    ) v (ProductCode, ProductName, Brand, Price, StockQuantity)
    WHERE f.FirmaKodu = 'DEMO3';
END
GO

IF NOT EXISTS (SELECT 1 FROM SiparisR WHERE SiparisNo = 'SIP-ORNEK-0001')
BEGIN
    DECLARE @firmaId INT = (SELECT FirmaId FROM Firma WHERE FirmaKodu = 'DEMO1');
    DECLARE @kullaniciId INT = (SELECT KullaniciId FROM Kullanici WHERE FirmaId = @firmaId AND KulAdi = 'kullanici');
    DECLARE @siparisId INT;

    INSERT INTO SiparisR (SiparisNo, FirmaId, KullaniciId, Tarih, BrutTutar, VergiTutar, GenelTutar, Notu, TeslimatAdresi, SiparisDurumu)
    VALUES ('SIP-ORNEK-0001', @firmaId, @kullaniciId, DATEADD(DAY, -5, GETDATE()), 1420.00, 284.00, 1704.00, NULL, 'Bağcılar / İstanbul', 'Delivered');

    SET @siparisId = SCOPE_IDENTITY();

    INSERT INTO SiparisD (SiparisId, UrunId, UrunKodu, UrunAdi, Miktar, BirimFiyat, BirimTutar, KDVOrani, KDVTutari, GenelToplam)
    SELECT @siparisId, ProductId, ProductCode, ProductName, 4, 185.00, 740.00, 20, 148.00, 888.00
    FROM Products WHERE FirmaId = @firmaId AND ProductCode = 'FLT-001';

    INSERT INTO SiparisD (SiparisId, UrunId, UrunKodu, UrunAdi, Miktar, BirimFiyat, BirimTutar, KDVOrani, KDVTutari, GenelToplam)
    SELECT @siparisId, ProductId, ProductCode, ProductName, 2, 340.00, 680.00, 20, 136.00, 816.00
    FROM Products WHERE FirmaId = @firmaId AND ProductCode = 'SIL-020';
END
GO

IF NOT EXISTS (SELECT 1 FROM SiparisR WHERE SiparisNo = 'SIP-ORNEK-0002')
BEGIN
    DECLARE @firmaId INT = (SELECT FirmaId FROM Firma WHERE FirmaKodu = 'DEMO1');
    DECLARE @kullaniciId INT = (SELECT KullaniciId FROM Kullanici WHERE FirmaId = @firmaId AND KulAdi = 'ahmet');
    DECLARE @siparisId INT;

    INSERT INTO SiparisR (SiparisNo, FirmaId, KullaniciId, Tarih, BrutTutar, VergiTutar, GenelTutar, Notu, TeslimatAdresi, SiparisDurumu)
    VALUES ('SIP-ORNEK-0002', @firmaId, @kullaniciId, DATEADD(DAY, -1, GETDATE()), 3250.00, 650.00, 3900.00, 'Öğleden önce teslim', 'Kadıköy / İstanbul', 'Pending');

    SET @siparisId = SCOPE_IDENTITY();

    INSERT INTO SiparisD (SiparisId, UrunId, UrunKodu, UrunAdi, Miktar, BirimFiyat, BirimTutar, KDVOrani, KDVTutari, GenelToplam)
    SELECT @siparisId, ProductId, ProductCode, ProductName, 1, 3250.00, 3250.00, 20, 650.00, 3900.00
    FROM Products WHERE FirmaId = @firmaId AND ProductCode = 'AKU-060';
END
GO

IF NOT EXISTS (SELECT 1 FROM SiparisR WHERE SiparisNo = 'SIP-ORNEK-0003')
BEGIN
    DECLARE @firmaId INT = (SELECT FirmaId FROM Firma WHERE FirmaKodu = 'DEMO2');
    DECLARE @kullaniciId INT = (SELECT KullaniciId FROM Kullanici WHERE FirmaId = @firmaId AND KulAdi = 'mehmet');
    DECLARE @siparisId INT;

    INSERT INTO SiparisR (SiparisNo, FirmaId, KullaniciId, Tarih, BrutTutar, VergiTutar, GenelTutar, Notu, TeslimatAdresi, SiparisDurumu)
    VALUES ('SIP-ORNEK-0003', @firmaId, @kullaniciId, DATEADD(DAY, -2, GETDATE()), 3620.00, 724.00, 4344.00, NULL, 'Adapazarı / Sakarya', 'Approved');

    SET @siparisId = SCOPE_IDENTITY();

    INSERT INTO SiparisD (SiparisId, UrunId, UrunKodu, UrunAdi, Miktar, BirimFiyat, BirimTutar, KDVOrani, KDVTutari, GenelToplam)
    SELECT @siparisId, ProductId, ProductCode, ProductName, 2, 760.00, 1520.00, 20, 304.00, 1824.00
    FROM Products WHERE FirmaId = @firmaId AND ProductCode = 'BUJ-004';

    INSERT INTO SiparisD (SiparisId, UrunId, UrunKodu, UrunAdi, Miktar, BirimFiyat, BirimTutar, KDVOrani, KDVTutari, GenelToplam)
    SELECT @siparisId, ProductId, ProductCode, ProductName, 1, 2100.00, 2100.00, 20, 420.00, 2520.00
    FROM Products WHERE FirmaId = @firmaId AND ProductCode = 'AMR-110';
END
GO
