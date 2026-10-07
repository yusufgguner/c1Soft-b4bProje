import { useCallback, useState } from 'react'
import { cikisYap, kayitliKullanici, type Kullanici } from './api'
import Giris from './Giris'
import Urunler from './Urunler'

export default function App() {
  const [kullanici, setKullanici] = useState<Kullanici | null>(kayitliKullanici)
  const [mesaj, setMesaj] = useState('')

  // Oturum düşünce giriş ekranına döner
  const oturumBitti = useCallback((yeniMesaj: string) => {
    setMesaj(yeniMesaj)
    setKullanici(null)
  }, [])

  if (!kullanici) {
    return (
      <Giris
        key={mesaj}
        mesaj={mesaj}
        onGiris={(k) => {
          setMesaj('')
          setKullanici(k)
        }}
      />
    )
  }

  return (
    <Urunler
      kullanici={kullanici}
      onOturumBitti={oturumBitti}
      onCikis={async () => {
        await cikisYap(kullanici)
        setKullanici(null)
      }}
    />
  )
}
