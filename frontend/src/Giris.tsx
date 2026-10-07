import { useState } from 'react'
import { girisYap, type Kullanici } from './api'

type Props = {
  onGiris: (kullanici: Kullanici) => void
  mesaj?: string
}

export default function Giris({ onGiris, mesaj }: Props) {
  const [firmaKodu, setFirmaKodu] = useState('')
  const [kulAdi, setKulAdi] = useState('')
  const [sifre, setSifre] = useState('')
  const [hata, setHata] = useState(mesaj ?? '')
  const [bekliyor, setBekliyor] = useState(false)

  // Formu gönderip giriş yapar
  async function gonder(e: React.FormEvent) {
    e.preventDefault()
    setHata('')
    setBekliyor(true)

    try {
      onGiris(await girisYap(firmaKodu.trim(), kulAdi.trim(), sifre))
    } catch (err) {
      setHata(err instanceof Error ? err.message : 'Giriş yapılamadı.')
    } finally {
      setBekliyor(false)
    }
  }

  return (
    <div className="giris-sayfa">
      <form className="giris-kutu" onSubmit={gonder}>
        <h1>B4B Giriş</h1>

        <label>
          Firma kodu
          <input value={firmaKodu} onChange={(e) => setFirmaKodu(e.target.value)} required autoFocus />
        </label>

        <label>
          Kullanıcı adı
          <input value={kulAdi} onChange={(e) => setKulAdi(e.target.value)} required />
        </label>

        <label>
          Şifre
          <input type="password" value={sifre} onChange={(e) => setSifre(e.target.value)} required />
        </label>

        {hata && <div className="hata">{hata}</div>}

        <button type="submit" disabled={bekliyor}>
          {bekliyor ? 'Giriş yapılıyor...' : 'Giriş yap'}
        </button>
      </form>
    </div>
  )
}
