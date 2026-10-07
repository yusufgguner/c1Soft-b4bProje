# urun arama hiz testi
# kullanim: .\tools\arama-benchmark.ps1 -FirmaKodu DEMO1 -KulAdi admin -Sifre ****
param(
    [string]$Adres = "http://localhost:5178",
    [Parameter(Mandatory = $true)][string]$FirmaKodu,
    [Parameter(Mandatory = $true)][string]$KulAdi,
    [Parameter(Mandatory = $true)][string]$Sifre
)

$giris = @{ firmaKodu = $FirmaKodu; kulAdi = $KulAdi; sifre = $Sifre } | ConvertTo-Json
$token = (Invoke-RestMethod -Method Post -Uri "$Adres/api/auth/login" -ContentType "application/json" -Body $giris).token
$baslik = @{ Authorization = "Bearer $token" }

# denenecek aramalar (her calistirmada farkli olsun diye sayi ekleniyor)
$kelimeler = @("balata", "blata", "fren diski", "yag filtresi", "hava filtresi", "amortisor", "buji", "triger", "radyator", "aku",
               "egea", "clio", "golf", "corolla", "bosch", "brembo", "mann", "sachs", "FRN-0", "YGF-001", "debriyaj", "rulman",
               "silecek", "far ampulu", "alternator")
$tur = Get-Random -Maximum 100000
$aramalar = foreach ($k in $kelimeler) { foreach ($s in 1..4) { "q=$([uri]::EscapeDataString($k))&sayfa=$s&adet=$(20 + ($tur % 7))" } }

$sonuclar = New-Object System.Collections.Generic.List[object]

foreach ($asama in @("soguk", "sicak")) {
    foreach ($a in $aramalar) {
        $cevap = Invoke-WebRequest -Uri "$Adres/api/products/search?$a" -Headers $baslik -UseBasicParsing
        $sonuclar.Add([pscustomobject]@{
            Asama  = $asama
            Kaynak = [string]$cevap.Headers["X-Search-Source"]
            Ms     = [double]::Parse([string]$cevap.Headers["X-Search-Ms"], [cultureinfo]::InvariantCulture)
        })
    }
}

$sonuclar | Group-Object Asama, Kaynak | ForEach-Object {
    $ms = $_.Group.Ms | Sort-Object
    [pscustomobject]@{
        Asama   = $_.Group[0].Asama
        Kaynak  = $_.Group[0].Kaynak
        Adet    = $ms.Count
        Ortalama = [math]::Round(($ms | Measure-Object -Average).Average, 3)
        P95     = [math]::Round($ms[[math]::Min($ms.Count - 1, [int][math]::Floor($ms.Count * 0.95))], 3)
        EnKotu  = [math]::Round($ms[-1], 3)
    }
} | Format-Table -AutoSize
