// Aviyonik ünite ile kullanıcı arayüzü arasındaki paket türlerini tanımlar.
// Paket başlığındaki "Paket ID" alanına bu değerlerden biri yazılır.
namespace OrtakKutuphane.Enumlar;

// Haberleşme protokolünde kullanılan paket tiplerini ifade eder.
// Değerler tek bir byte (u8) içine sığacak şekilde tanımlanmıştır.
public enum UnitePaketTipleri : byte
{
    // Aviyonikten kullanıcı arayüzüne giden ilk telemetri paketi.
    HaberlesmePaket1 = 0x01,

    // Aviyonikten kullanıcı arayüzüne giden ikinci telemetri paketi.
    HaberlesmePaket2 = 0x02,

    // Kullanıcı arayüzünden aviyoniğe gönderilen komut paketi.
    KomutPaket = 0x03,

    // Kullanıcı arayüzünden aviyoniğe gönderilen ayar paketi.
    AyarPaket = 0x04,

    // Aviyonikten gelen, ayar/komut sorgusuna verilen geri besleme paketi.
    GeriBeslemePaket = 0x05
}
