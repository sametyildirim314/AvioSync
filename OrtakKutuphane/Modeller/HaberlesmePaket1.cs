using System.Runtime.InteropServices;

namespace OrtakKutuphane.Modeller;

// Aviyonikten kullanıcı arayüzüne 5 Hz frekansla gönderilen birinci telemetri paketi.
// Veri kısmı toplam 15 byte uzunluğundadır ve Little Endian olarak iletilir.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HaberlesmePaket1
{
    // 1 byte işaretsiz veri (0-255 arası).
    public byte Veri1_u8;

    // 1 byte işaretsiz veri.
    public byte Veri2_u8;

    // 1 byte işaretsiz veri.
    public byte Veri3_u8;

    // 2 byte işaretli veri (-32768 ile 32767 arası).
    public short Veri4_s16;

    // 2 byte işaretli veri.
    public short Veri5_s16;

    // 4 byte ondalık kayan nokta veri (IEEE-754 single).
    public float Veri6_f32;

    // 4 byte ondalık kayan nokta veri (IEEE-754 single).
    public float Veri7_f32;

    // Veri kısmının protokoldeki sabit boyutu (byte).
    public const byte VeriBoyutu = 15;
}
