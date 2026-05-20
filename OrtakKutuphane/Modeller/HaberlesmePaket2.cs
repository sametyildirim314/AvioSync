using System.Runtime.InteropServices;

namespace OrtakKutuphane.Modeller;

// Aviyonikten kullanıcı arayüzüne 5 Hz frekansla gönderilen ikinci telemetri paketi.
// Veri kısmı toplam 20 byte uzunluğundadır ve Little Endian olarak iletilir.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HaberlesmePaket2
{
    // 1 byte işaretsiz veri.
    public byte Veri1_u8;

    // 1 byte işaretsiz veri.
    public byte Veri2_u8;

    // 2 byte işaretli veri.
    public short Veri3_s16;

    // 4 byte işaretli veri (-2^31 ile 2^31-1 arası).
    public int Veri4_s32;

    // 4 byte işaretsiz sistem zamanı (ms olarak yorumlanabilir).
    public uint SistemZamani_u32;

    // 8 byte yüksek hassasiyetli ondalık veri (IEEE-754 double).
    public double Veri6_f64;

    // Veri kısmının protokoldeki sabit boyutu (byte).
    public const byte VeriBoyutu = 20;
}
