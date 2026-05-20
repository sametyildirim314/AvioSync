namespace OrtakKutuphane.Protokol;

// Haberleşme protokolünün sabit byte değerlerini ve boyutlarını içerir.
// Tek bir yerden yönetildiği için DRY prensibine uygun olarak korunur.
public static class ProtokolSabitleri
{
    // Birinci senkronizasyon byte değeri (0xA9 = 169).
    public const byte Senkron1 = 169;

    // İkinci senkronizasyon byte değeri (0xE9 = 233).
    public const byte Senkron2 = 233;

    // Başlık boyutu: Senkron1 + Senkron2 + PaketID + PaketBoyutu = 4 byte.
    public const int BaslikBoyutu = 4;

    // CRC alanının boyutu (byte).
    public const int CrcBoyutu = 1;

    // Bir paketin minimum toplam boyutu: Başlık + CRC = 5 byte (veri yokken).
    public const int MinimumPaketBoyutu = BaslikBoyutu + CrcBoyutu;

    // Paket boyutu alanının (1 byte) izin verdiği maksimum veri boyutu.
    public const int MaksimumVeriBoyutu = 255;

    // CRC-8 algoritması için seçilen polinom (CRC-8/CCITT, 0x07).
    public const byte Crc8Polinomu = 0x07;
}
