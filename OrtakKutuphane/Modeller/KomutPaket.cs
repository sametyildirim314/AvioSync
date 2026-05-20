using OrtakKutuphane.Enumlar;
using System.Runtime.InteropServices;

namespace OrtakKutuphane.Modeller;

// Kullanıcı arayüzünden aviyoniğe gönderilen tek bytelık komut paketi.
// Toplam veri boyutu 1 byte olup, komut tipi KomutTipi enum'u ile ifade edilir.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KomutPaket
{
    // Gönderilen komutun türü.
    public KomutTipi Komut;

    // Veri kısmının protokoldeki sabit boyutu (byte).
    public const byte VeriBoyutu = 1;
}
