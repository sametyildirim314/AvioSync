using OrtakKutuphane.Enumlar;
using System.Runtime.InteropServices;

namespace OrtakKutuphane.Modeller;

// Kullanıcı arayüzünden aviyoniğe gönderilen 5 byte uzunluğundaki ayar paketi.
// 1 byte ayar tipi + 4 byte ondalık değer (Little Endian) yapısındadır.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AyarPaket
{
    // Hangi ayarın değiştirileceğini belirten tip.
    public AyarTipi Ayar;

    // İlgili ayar için atanmak istenen değer.
    public float Deger_f32;

    // Veri kısmının protokoldeki sabit boyutu (byte).
    public const byte VeriBoyutu = 5;
}
