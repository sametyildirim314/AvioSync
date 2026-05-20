namespace OrtakKutuphane.Protokol;

// CRC-8 hesaplamasını gerçekleştiren statik yardımcı sınıf.
// Seçilen algoritma: CRC-8 / CCITT — polinom 0x07, başlangıç değeri 0x00, XOR-Out 0x00.
// Tek seferlik bir lookup tablosu üretilip cache'lenir; bu sayede gerçek zamanlı
// haberleşmede paket başına O(N) byte için O(N) hesap maliyeti sabit kalır.
public static class Crc8Hesaplayici
{
    // Algoritma: CRC-8/CCITT
    // Polinom (x^8 + x^2 + x + 1) -> 0x07
    // Initial Value: 0x00, XorOut: 0x00, RefIn: false, RefOut: false
    private static readonly byte[] _lookupTablosu = LookupTablosuOlustur();

    // Verilen byte dizisinin tamamı için CRC-8 değerini hesaplar.
    // veri: CRC hesaplanacak veri tamponu.
    // Döner: Hesaplanan 1 byte CRC sonucu.
    public static byte Hesapla(ReadOnlySpan<byte> veri)
    {
        byte crc = 0x00;
        for (int i = 0; i < veri.Length; i++)
        {
            crc = _lookupTablosu[crc ^ veri[i]];
        }
        return crc;
    }

    // Belirtilen aralık için CRC-8 değerini hesaplar.
    // Senkron + ID + boyut + veri bölümü için kullanıldığında pratik bir kısayoldur.
    public static byte Hesapla(byte[] tampon, int baslangic, int uzunluk)
    {
        if (tampon is null) throw new ArgumentNullException(nameof(tampon));
        if (baslangic < 0 || uzunluk < 0 || baslangic + uzunluk > tampon.Length)
            throw new ArgumentOutOfRangeException(nameof(uzunluk), "CRC aralığı tampon sınırlarının dışında.");

        return Hesapla(new ReadOnlySpan<byte>(tampon, baslangic, uzunluk));
    }

    // 256 girişlik CRC-8 lookup tablosunu oluşturur.
    private static byte[] LookupTablosuOlustur()
    {
        var tablo = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            byte gecici = (byte)i;
            for (int bit = 0; bit < 8; bit++)
            {
                // En anlamlı bit (MSB) 1 ise sola kaydır + polinom ile XOR
                gecici = (gecici & 0x80) != 0
                    ? (byte)((gecici << 1) ^ ProtokolSabitleri.Crc8Polinomu)
                    : (byte)(gecici << 1);
            }
            tablo[i] = gecici;
        }
        return tablo;
    }
}
