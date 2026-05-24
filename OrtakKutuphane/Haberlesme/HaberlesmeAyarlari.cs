namespace OrtakKutuphane.Haberlesme;

// İki uygulama arasındaki UDP haberleşmesi için IP/port konfigürasyonu.
// Aviyonik (simülasyon) ve kullanıcı arayüzü, kendi tarafının ayarlarını oluşturup paylaşırlar.
public sealed class HaberlesmeAyarlari
{
    // UDP soketinin yerelde bağlanacağı (Bind) port. Bu porttan gelen paketler dinlenir.
    public int YerelDinlemePortu { get; init; }

    // Karşı tarafın IP adresi. Localhost ise "127.0.0.1".
    public string HedefIp { get; init; } = "127.0.0.1";

    // Karşı tarafın UDP portu. Gönderilen paketler bu adrese iletilir.
    public int HedefPort { get; init; }

    // Yerel arayüzde dinleme için kullanılacak IP. Varsayılan: tüm arayüzler.
    public string YerelDinlemeIp { get; init; } = "0.0.0.0";

    // Varsayılan port konfigürasyonu: Kullanıcı arayüzü 5001'i dinler, simülasyon 5002'yi dinler.
    public const int VarsayilanKullaniciArayuzuPortu = 5001;
    public const int VarsayilanSimulasyonPortu = 5002;

    // Kullanıcı arayüzü tarafı için hazır ayar nesnesi üretir.
    public static HaberlesmeAyarlari KullaniciArayuzuIcin(string hedefIp = "127.0.0.1") => new()
    {
        YerelDinlemePortu = VarsayilanKullaniciArayuzuPortu,
        HedefIp = hedefIp,
        HedefPort = VarsayilanSimulasyonPortu
    };

    // Simülasyon arayüzü tarafı için hazır ayar nesnesi üretir.
    public static HaberlesmeAyarlari SimulasyonArayuzuIcin(string hedefIp = "127.0.0.1") => new()
    {
        YerelDinlemePortu = VarsayilanSimulasyonPortu,
        HedefIp = hedefIp,
        HedefPort = VarsayilanKullaniciArayuzuPortu
    };
}
