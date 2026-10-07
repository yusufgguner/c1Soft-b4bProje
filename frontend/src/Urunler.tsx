import { useEffect, useState } from 'react'
import { OturumHatasi, urunAra, type AramaCevabi, type Kullanici } from './api'

type Props = {
  kullanici: Kullanici
  onCikis: () => void
  onOturumBitti: (mesaj: string) => void
}

const ADET = 20

export default function Urunler({ kullanici, onCikis, onOturumBitti }: Props) {
  const [arama, setArama] = useState('')
  const [sayfa, setSayfa] = useState(1)
  const [cevap, setCevap] = useState<AramaCevabi | null>(null)
  const [hata, setHata] = useState('')

  useEffect(() => {
    let iptal = false

    const zamanlayici = setTimeout(async () => {
      try {
        const sonuc = await urunAra(kullanici, arama.trim(), sayfa)
        if (!iptal) {
          setCevap(sonuc)
          setHata('')
        }
      } catch (err) {
        if (err instanceof OturumHatasi) {
          onOturumBitti(err.message)
        } else if (!iptal) {
          setHata(err instanceof Error ? err.message : 'Arama yapılamadı.')
        }
      }
    }, 150)

    return () => {
      iptal = true
      clearTimeout(zamanlayici)
    }
  }, [arama, sayfa, kullanici, onOturumBitti])

  const toplamSayfa = cevap ? Math.max(1, Math.ceil(cevap.sonuc.toplam / ADET)) : 1

  return (
    <div className="sayfa">
      <header className="ust">
        <strong>{kullanici.firmaAdi}</strong>
        <span>
          {kullanici.adSoyad || kullanici.kulAdi}
          <button className="cikis" onClick={onCikis}>
            Çıkış
          </button>
        </span>
      </header>

      <main>
        <input
          className="arama"
          placeholder="Ürün kodu, adı veya marka ara..."
          value={arama}
          onChange={(e) => {
            setArama(e.target.value)
            setSayfa(1)
          }}
          autoFocus
        />

        <div className="bilgi">
          {cevap && (
            <>
              <span>{cevap.sonuc.toplam.toLocaleString('tr-TR')} ürün</span>
              <span className="hiz">
                {cevap.ms} ms · {cevap.kaynak}
              </span>
            </>
          )}
        </div>

        {hata && <div className="hata">{hata}</div>}

        <table>
          <thead>
            <tr>
              <th>Kod</th>
              <th>Ürün</th>
              <th>Marka</th>
              <th className="sag">Fiyat</th>
              <th className="sag">Stok</th>
            </tr>
          </thead>
          <tbody>
            {cevap?.sonuc.urunler.map((u) => (
              <tr key={u.productId}>
                <td>{u.productCode}</td>
                <td>{u.productName}</td>
                <td>{u.brand}</td>
                <td className="sag">{u.price.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })}</td>
                <td className={u.stockQuantity === 0 ? 'sag yok' : 'sag'}>{u.stockQuantity}</td>
              </tr>
            ))}
            {cevap && cevap.sonuc.urunler.length === 0 && (
              <tr>
                <td colSpan={5} className="bos">
                  Ürün bulunamadı.
                </td>
              </tr>
            )}
          </tbody>
        </table>

        <div className="sayfalama">
          <button disabled={sayfa <= 1} onClick={() => setSayfa(sayfa - 1)}>
            Önceki
          </button>
          <span>
            {sayfa} / {toplamSayfa}
          </span>
          <button disabled={sayfa >= toplamSayfa} onClick={() => setSayfa(sayfa + 1)}>
            Sonraki
          </button>
        </div>
      </main>
    </div>
  )
}
