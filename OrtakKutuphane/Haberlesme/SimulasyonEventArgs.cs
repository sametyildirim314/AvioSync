using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;

namespace OrtakKutuphane.Haberlesme;

// LED durumunun değiştiğini UI'a bildirmek için event argümanı.
public sealed class LedDurumuDegistiEventArgs : EventArgs
{
    // LED yanık mı söndü mü?
    public bool YanikMi { get; }

    // Durumu tetikleyen ayarın türü (loglama/görsel ipucu için).
    public AyarTipi TetikleyenAyar { get; }

    // Ayarın yeni değeri.
    public float YeniDeger { get; }

    public LedDurumuDegistiEventArgs(bool yanikMi, AyarTipi tetikleyenAyar, float yeniDeger)
    {
        YanikMi = yanikMi;
        TetikleyenAyar = tetikleyenAyar;
        YeniDeger = yeniDeger;
    }
}

// Komut paketi işlendiğinde UI'a bildirmek için event argümanı.
public sealed class KomutAlindiEventArgs : EventArgs
{
    public KomutTipi Komut { get; }

    public KomutAlindiEventArgs(KomutTipi komut)
    {
        Komut = komut;
    }
}

// Telemetri paketi gönderildiğinde UI'a bildirmek için event argümanı.
public sealed class TelemetriGonderildiEventArgs : EventArgs
{
    public UnitePaketTipleri PaketTipi { get; }

    // İsteğe bağlı: gönderilen paket içeriklerinden bazıları UI'da gösterilebilsin.
    public HaberlesmePaket1? Paket1 { get; }
    public HaberlesmePaket2? Paket2 { get; }

    public TelemetriGonderildiEventArgs(HaberlesmePaket1 paket)
    {
        PaketTipi = UnitePaketTipleri.HaberlesmePaket1;
        Paket1 = paket;
    }

    public TelemetriGonderildiEventArgs(HaberlesmePaket2 paket)
    {
        PaketTipi = UnitePaketTipleri.HaberlesmePaket2;
        Paket2 = paket;
    }
}
