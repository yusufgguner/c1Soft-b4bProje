(function () {
    const durumlar = {
        Pending: ["Bekliyor", "bg-yellow-lt"],
        Approved: ["Onaylandı", "bg-blue-lt"],
        Shipped: ["Kargoda", "bg-purple-lt"],
        Delivered: ["Teslim edildi", "bg-green-lt"],
        Rejected: ["Reddedildi", "bg-red-lt"],
        Cancelled: ["İptal", "bg-secondary-lt"]
    };
    const para = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const durumEtiketi = document.getElementById("canli-durum");
    const sayac = document.getElementById("yeni-siparis-sayaci");
    let yeniSayisi = 0;

    function yaziGuvenli(metin) {
        const div = document.createElement("div");
        div.textContent = metin ?? "";
        return div.innerHTML;
    }

    function durumRozeti(durum) {
        const [ad, renk] = durumlar[durum] ?? [durum, "bg-secondary-lt"];
        return `<span class="badge ${renk}">${yaziGuvenli(ad)}</span>`;
    }

    function tarihYaz(deger) {
        const t = new Date(deger);
        const iki = n => String(n).padStart(2, "0");
        return `${iki(t.getDate())}.${iki(t.getMonth() + 1)}.${t.getFullYear()} ${iki(t.getHours())}:${iki(t.getMinutes())}`;
    }

    function baglantiDurumu(yazi, renk) {
        if (durumEtiketi) {
            durumEtiketi.className = `badge ${renk} me-3`;
            durumEtiketi.innerHTML = `<i class="ti ti-point-filled"></i> ${yazi}`;
        }
    }

    function bildirimGoster(s) {
        const kutu = document.getElementById("bildirimler");
        if (!kutu) return;
        const toast = document.createElement("div");
        toast.className = "toast show";
        toast.innerHTML = `
            <div class="toast-header">
                <span class="avatar avatar-xs bg-green text-white me-2"><i class="ti ti-shopping-cart"></i></span>
                <strong class="me-auto">Yeni sipariş</strong>
                <small>şimdi</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                <a href="/admin/siparisler/detay/${s.siparisId}" class="fw-medium">${yaziGuvenli(s.siparisNo)}</a><br>
                ${yaziGuvenli(s.firmaKodu)} / ${yaziGuvenli(s.kulAdi)} - ${para.format(s.genelTutar)} ₺
            </div>`;
        toast.querySelector(".btn-close").addEventListener("click", () => toast.remove());
        kutu.prepend(toast);
        setTimeout(() => toast.remove(), 8000);
    }

    function satirEkle(s) {
        const tablo = document.getElementById("canli-siparisler");
        if (!tablo) return false;
        if (tablo.dataset.firmaId && tablo.dataset.firmaId !== String(s.firmaId)) return false;
        if (tablo.dataset.durum && tablo.dataset.durum !== s.siparisDurumu) return false;

        tablo.querySelector(".bos-satir")?.remove();
        const satir = document.createElement("tr");
        satir.dataset.siparisId = s.siparisId;
        satir.className = "canli-yeni";
        satir.innerHTML = `
            <td><a href="/admin/siparisler/detay/${s.siparisId}" class="fw-medium">${yaziGuvenli(s.siparisNo)}</a></td>
            <td><span class="badge bg-azure-lt">${yaziGuvenli(s.firmaKodu)}</span></td>
            <td>${yaziGuvenli(s.kulAdi)}</td>
            <td class="text-secondary">${tarihYaz(s.tarih)}</td>
            <td class="text-end">${s.kalemSayisi}</td>
            <td class="text-end fw-medium">${para.format(s.genelTutar)} ₺</td>
            <td class="siparis-durum">${durumRozeti(s.siparisDurumu)}</td>`;
        tablo.prepend(satir);

        const toplam = tablo.closest(".card")?.querySelector(".toplam-kayit");
        if (toplam) toplam.textContent = parseInt(toplam.textContent, 10) + 1;

        const enFazla = parseInt(tablo.dataset.enFazla || "0", 10);
        while (enFazla > 0 && tablo.rows.length > enFazla) {
            tablo.deleteRow(tablo.rows.length - 1);
        }
        return true;
    }

    function panelSayilariniArtir(s) {
        const adet = document.getElementById("bugun-siparis");
        const ciro = document.getElementById("bugun-ciro");
        if (adet) adet.textContent = parseInt(adet.textContent, 10) + 1;
        if (ciro) {
            const mevcut = parseFloat(ciro.textContent.replace(/\./g, "").replace(",", ".")) || 0;
            ciro.textContent = para.format(mevcut + s.genelTutar);
        }
    }

    const baglanti = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/siparis")
        .withAutomaticReconnect()
        .build();

    baglanti.on("YeniSiparis", s => {
        bildirimGoster(s);
        panelSayilariniArtir(s);
        if (!satirEkle(s) && sayac) {
            yeniSayisi++;
            sayac.textContent = yeniSayisi;
            sayac.classList.remove("d-none");
        }
    });

    baglanti.on("SiparisDurumDegisti", s => {
        const hucre = document.querySelector(`tr[data-siparis-id="${s.siparisId}"] .siparis-durum`);
        if (hucre) {
            hucre.innerHTML = durumRozeti(s.siparisDurumu);
            hucre.parentElement.classList.remove("canli-yeni");
            void hucre.parentElement.offsetWidth;
            hucre.parentElement.classList.add("canli-yeni");
        }
    });

    baglanti.onreconnecting(() => baglantiDurumu("Yeniden bağlanıyor", "bg-yellow-lt"));
    baglanti.onreconnected(() => baglantiDurumu("Canlı", "bg-green-lt"));
    baglanti.onclose(() => baglantiDurumu("Bağlantı yok", "bg-red-lt"));

    baglanti.start()
        .then(() => baglantiDurumu("Canlı", "bg-green-lt"))
        .catch(() => baglantiDurumu("Bağlantı yok", "bg-red-lt"));
})();
