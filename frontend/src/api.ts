export type Kullanici = {
  token: string
  kulAdi: string
  adSoyad: string
  firmaKodu: string
  firmaAdi: string
}

export type Urun = {
  productId: number
  productCode: string
  productName: string
  brand: string | null
  price: number
  stockQuantity: number
  isActive: boolean
}

export type AramaSonucu = {
  toplam: number
  sayfa: number
  urunler: Urun[]
}

export type AramaCevabi = {
  sonuc: AramaSonucu
  ms: string
  kaynak: string
}

const ANAHTAR = 'b4b-kullanici'

export class OturumHatasi extends Error {}

// Kayıtlı kullanıcıyı getirir
export function kayitliKullanici(): Kullanici | null {
  const deger = localStorage.getItem(ANAHTAR)
  return deger ? (JSON.parse(deger) as Kullanici) : null
}

// Giriş yapar ve kullanıcıyı kaydeder
export async function girisYap(firmaKodu: string, kulAdi: string, sifre: string): Promise<Kullanici> {
  const cevap = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ firmaKodu, kulAdi, sifre }),
  })

  const veri = await cevap.json()

  if (!cevap.ok) {
    throw new Error(veri.mesaj ?? 'Giriş yapılamadı.')
  }

  const kullanici: Kullanici = {
    token: veri.token,
    kulAdi: veri.kulAdi,
    adSoyad: veri.adSoyad,
    firmaKodu: veri.firmaKodu,
    firmaAdi: veri.firmaAdi,
  }
  localStorage.setItem(ANAHTAR, JSON.stringify(kullanici))
  return kullanici
}

// Çıkış yapar
export async function cikisYap(kullanici: Kullanici) {
  try {
    await fetch('/api/auth/logout', { method: 'POST', headers: { Authorization: `Bearer ${kullanici.token}` } })
  } finally {
    localStorage.removeItem(ANAHTAR)
  }
}

// Ürün arar, süreyi ve kaynağı da döner
export async function urunAra(kullanici: Kullanici, q: string, sayfa: number): Promise<AramaCevabi> {
  const parametre = new URLSearchParams({ q, sayfa: String(sayfa), adet: '20' })
  const cevap = await fetch(`/api/products/search?${parametre}`, {
    headers: { Authorization: `Bearer ${kullanici.token}` },
  })

  if (cevap.status === 401) {
    localStorage.removeItem(ANAHTAR)
    const veri = await cevap.json().catch(() => null)
    throw new OturumHatasi(veri?.mesaj ?? 'Oturumunuz sona erdi, tekrar giriş yapın.')
  }

  if (!cevap.ok) {
    throw new Error('Arama yapılamadı.')
  }

  return {
    sonuc: await cevap.json(),
    ms: cevap.headers.get('X-Search-Ms') ?? '',
    kaynak: cevap.headers.get('X-Search-Source') ?? '',
  }
}
