using OrtakKutuphane.Enumlar;

namespace OrtakKutuphane.Haberlesme;

// Paket yakalama makinesinden başarıyla çözülmüş bir paketi temsil eder.
// Veri alanı yalnızca paketin "veri dizisi" bölümünü içerir (başlık ve CRC hariç).
public sealed class YakalananPaket
{
    // Paketin türü (Paket ID byte'ından çözülen enum değeri).
    public UnitePaketTipleri PaketTipi { get; }

    // Paket içindeki veri dizisi (Little Endian byte'lar). Başlık ve CRC dahil değildir.
    public byte[] Veri { get; }

    // Paketin yakalandığı an (UTC).
    public DateTime YakalanmaZamani { get; }

    public YakalananPaket(UnitePaketTipleri paketTipi, byte[] veri)
    {
        PaketTipi = paketTipi;
        Veri = veri ?? throw new ArgumentNullException(nameof(veri));
        YakalanmaZamani = DateTime.UtcNow;
    }
}
