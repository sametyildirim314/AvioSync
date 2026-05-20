using OrtakKutuphane.Enumlar;
using System.Runtime.InteropServices;

namespace OrtakKutuphane.Modeller;

// Aviyonikten kullanıcı arayüzüne gönderilen 5 byte uzunluğundaki geri besleme paketi.
// İlgili ayar sorgusunun veya komutun sonucunu taşır.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GeriBeslemePaket
{
    // Geri beslemenin hangi sorguya/komuta ait olduğunu belirtir.
    public AyarSorguTipi CevapTipi;

    // Cevap olarak iletilen güncel değer.
    public float Deger_f32;

    // Veri kısmının protokoldeki sabit boyutu (byte).
    public const byte VeriBoyutu = 5;
}
