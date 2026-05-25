using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Haberlesme;
using OrtakKutuphane.Modeller;
using OrtakKutuphane.Protokol;

namespace KullaniciArayuzu.Haberlesme;

// Aviyonikten gelen HaberlesmePaket1/2 paketleri için event argümanı.
public sealed class HaberlesmePaket1AlindiEventArgs : EventArgs
{
    public HaberlesmePaket1 Paket { get; }
    public HaberlesmePaket1AlindiEventArgs(HaberlesmePaket1 paket) => Paket = paket;
}

public sealed class HaberlesmePaket2AlindiEventArgs : EventArgs
{
    public HaberlesmePaket2 Paket { get; }
    public HaberlesmePaket2AlindiEventArgs(HaberlesmePaket2 paket) => Paket = paket;
}

public sealed class GeriBeslemeAlindiEventArgs : EventArgs
{
    public GeriBeslemePaket Paket { get; }
    public GeriBeslemeAlindiEventArgs(GeriBeslemePaket paket) => Paket = paket;
}

// Kullanıcı arayüzünün aviyonik simülasyonu ile haberleşmesini yöneten servis sınıfı.
// UdpHaberlesmeci'yi sarmalayıp paket türlerine göre özel event'ler yayınlar.
// Komut ve Ayar paketlerini gönderme arayüzü sunar.
public sealed class KullaniciIstemcisi : IDisposable
{
    private readonly UdpHaberlesmeci _haberlesmeci;

    public PaketYakalamaMakinesi Yakalama => _haberlesmeci.Yakalama;
    public bool CalisiyorMu => _haberlesmeci.CalisiyorMu;

    public event EventHandler<HaberlesmePaket1AlindiEventArgs>? HaberlesmePaket1Alindi;
    public event EventHandler<HaberlesmePaket2AlindiEventArgs>? HaberlesmePaket2Alindi;
    public event EventHandler<GeriBeslemeAlindiEventArgs>? GeriBeslemeAlindi;
    public event EventHandler<HataliPaketYakalandiEventArgs>? HataliPaketYakalandi;

    public KullaniciIstemcisi(HaberlesmeAyarlari ayarlar)
    {
        _haberlesmeci = new UdpHaberlesmeci(ayarlar);
        _haberlesmeci.Yakalama.PaketYakalandi += PaketYakalandi;
        _haberlesmeci.Yakalama.HataliPaketYakalandi += HataliPaketYakalandiIlet;
    }

    public void Baslat() => _haberlesmeci.Baslat();
    public void Durdur() => _haberlesmeci.Durdur();

    // Aviyoniğe komut gönderir.
    public void KomutGonder(KomutTipi komut)
    {
        var paket = new KomutPaket { Komut = komut };
        _haberlesmeci.Gonder(PaketSerilestirici.Serilestir(paket));
    }

    // Aviyoniğe ayar gönderir (değer ile birlikte).
    public void AyarGonder(AyarTipi ayar, float deger)
    {
        var paket = new AyarPaket { Ayar = ayar, Deger_f32 = deger };
        _haberlesmeci.Gonder(PaketSerilestirici.Serilestir(paket));
    }

    // Yakalama makinesi başarılı paket çözünce çağrılır; tipe göre uygun event'i yayınlar.
    private void PaketYakalandi(object? gonderen, PaketYakalandiEventArgs e)
    {
        switch (e.Paket.PaketTipi)
        {
            case UnitePaketTipleri.HaberlesmePaket1:
                var p1 = PaketSerilestirici.HaberlesmePaket1Coz(e.Paket.Veri);
                HaberlesmePaket1Alindi?.Invoke(this, new HaberlesmePaket1AlindiEventArgs(p1));
                break;

            case UnitePaketTipleri.HaberlesmePaket2:
                var p2 = PaketSerilestirici.HaberlesmePaket2Coz(e.Paket.Veri);
                HaberlesmePaket2Alindi?.Invoke(this, new HaberlesmePaket2AlindiEventArgs(p2));
                break;

            case UnitePaketTipleri.GeriBeslemePaket:
                var gb = PaketSerilestirici.GeriBeslemePaketCoz(e.Paket.Veri);
                GeriBeslemeAlindi?.Invoke(this, new GeriBeslemeAlindiEventArgs(gb));
                break;
        }
    }

    private void HataliPaketYakalandiIlet(object? gonderen, HataliPaketYakalandiEventArgs e)
        => HataliPaketYakalandi?.Invoke(this, e);

    public void Dispose()
    {
        _haberlesmeci.Yakalama.PaketYakalandi -= PaketYakalandi;
        _haberlesmeci.Yakalama.HataliPaketYakalandi -= HataliPaketYakalandiIlet;
        _haberlesmeci.Dispose();
    }
}
