using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;

namespace OrtakKutuphane.Loglama;

// Bir telemetri paketinin (Paket1 veya Paket2) tek bir log kaydını temsil eden
// JSON serileştirilebilir DTO.
public sealed class LogKaydi
{
    // Paketin kullanıcı arayüzünce alındığı zaman (UTC).
    public DateTime Zaman { get; set; }

    // Paket tipi (sadece HaberlesmePaket1 veya HaberlesmePaket2 loglanır).
    public UnitePaketTipleri PaketTipi { get; set; }

    // PaketTipi = HaberlesmePaket1 ise dolu, aksi halde null.
    public HaberlesmePaket1? Paket1 { get; set; }

    // PaketTipi = HaberlesmePaket2 ise dolu, aksi halde null.
    public HaberlesmePaket2? Paket2 { get; set; }

    public static LogKaydi Olustur(HaberlesmePaket1 paket) => new()
    {
        Zaman = DateTime.UtcNow,
        PaketTipi = UnitePaketTipleri.HaberlesmePaket1,
        Paket1 = paket
    };

    public static LogKaydi Olustur(HaberlesmePaket2 paket) => new()
    {
        Zaman = DateTime.UtcNow,
        PaketTipi = UnitePaketTipleri.HaberlesmePaket2,
        Paket2 = paket
    };
}
