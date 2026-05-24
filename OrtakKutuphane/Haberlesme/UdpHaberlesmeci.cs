using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OrtakKutuphane.Haberlesme;

// UDP üzerinden paket gönderimi ve alımını yöneten sınıf.
// Tek bir özel dinleyici thread'i kullanır (3 thread limitine uyum için). Gönderim,
// çağıran thread üzerinden senkron olarak yapılır ve ayrı bir thread oluşturmaz.
// Gelen tüm byte'lar PaketYakalamaMakinesi'ne yönlendirilir.
public sealed class UdpHaberlesmeci : IDisposable
{
    private readonly HaberlesmeAyarlari _ayarlar;
    private readonly PaketYakalamaMakinesi _yakalama;
    private readonly IPEndPoint _hedefEndPoint;

    private UdpClient? _udpClient;
    private Thread? _dinleyiciThread;
    private volatile bool _calisiyorMu;

    // Sınıfın kullandığı paket yakalama makinesini dışarıya açar (sayaçlar + event'ler için).
    public PaketYakalamaMakinesi Yakalama => _yakalama;

    // Haberleşme şu an aktif (dinleyici çalışıyor) mu?
    public bool CalisiyorMu => _calisiyorMu;

    public UdpHaberlesmeci(HaberlesmeAyarlari ayarlar)
        : this(ayarlar, new PaketYakalamaMakinesi())
    {
    }

    public UdpHaberlesmeci(HaberlesmeAyarlari ayarlar, PaketYakalamaMakinesi yakalama)
    {
        _ayarlar = ayarlar ?? throw new ArgumentNullException(nameof(ayarlar));
        _yakalama = yakalama ?? throw new ArgumentNullException(nameof(yakalama));
        _hedefEndPoint = new IPEndPoint(IPAddress.Parse(_ayarlar.HedefIp), _ayarlar.HedefPort);
    }

    // UDP dinleyicisini başlatır. Bu metod ayrıca yeni bir Thread oluşturur (1 adet).
    public void Baslat()
    {
        if (_calisiyorMu) return;

        var dinlemeEndPoint = new IPEndPoint(
            IPAddress.Parse(_ayarlar.YerelDinlemeIp),
            _ayarlar.YerelDinlemePortu);

        _udpClient = new UdpClient();
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(dinlemeEndPoint);

        _calisiyorMu = true;

        _dinleyiciThread = new Thread(DinleyiciDongusu)
        {
            IsBackground = true,
            Name = $"UDP-Dinleyici:{_ayarlar.YerelDinlemePortu}"
        };
        _dinleyiciThread.Start();
    }

    // Dinleyiciyi nazikçe durdurur.
    public void Durdur()
    {
        if (!_calisiyorMu) return;

        _calisiyorMu = false;

        try
        {
            // Bloklanmış Receive'i serbest bırakmak için soketi kapatıyoruz.
            _udpClient?.Close();
        }
        catch
        {
            // Kapatma hatası kritik değil, geçilebilir.
        }

        // Thread'in temiz çıkması için kısa süre bekle.
        _dinleyiciThread?.Join(TimeSpan.FromSeconds(1));
        _dinleyiciThread = null;
        _udpClient = null;
    }

    // Hazır bir paketi (byte dizisi) UDP üzerinden hedefe gönderir. Çağıran thread'de çalışır.
    public void Gonder(byte[] paket)
    {
        if (paket is null) throw new ArgumentNullException(nameof(paket));
        if (paket.Length == 0) return;

        // Gönderim için ayrı bir UdpClient yerine; varsa dinleyici client'ı kullanırız (DRY).
        // Aksi halde geçici, dinleyici yokken de gönderim mümkün olsun diye yeni bir client açılır.
        if (_udpClient is not null)
        {
            _udpClient.Send(paket, paket.Length, _hedefEndPoint);
            return;
        }

        using var geciciClient = new UdpClient();
        geciciClient.Send(paket, paket.Length, _hedefEndPoint);
    }

    // Dinleyici thread'inin ana döngüsü. Soket kapatılana kadar paket okur.
    private void DinleyiciDongusu()
    {
        var uzakEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (_calisiyorMu && _udpClient is not null)
        {
            try
            {
                byte[] gelenVeri = _udpClient.Receive(ref uzakEndPoint);
                if (gelenVeri.Length > 0)
                {
                    // Yakalama makinesi paketleri state geçişleri ile çözüp event'leri yayınlar.
                    _yakalama.Besle(gelenVeri);
                }
            }
            catch (ObjectDisposedException)
            {
                // Durdurma sırasında soketin kapatılması -> beklenen son.
                break;
            }
            catch (SocketException)
            {
                // Geçici soket hatası; eğer hala çalışmamız gerekiyorsa devam, yoksa çık.
                if (!_calisiyorMu) break;
            }
        }
    }

    public void Dispose()
    {
        Durdur();
    }
}
