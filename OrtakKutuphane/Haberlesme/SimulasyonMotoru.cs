using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;
using OrtakKutuphane.Protokol;
using System.Diagnostics;
using System.Threading;

namespace OrtakKutuphane.Haberlesme;

// Aviyonik ünitesini simüle eden motor sınıfı.
// - 5 Hz frekansla HaberlesmePaket1 ve HaberlesmePaket2 üretip UDP üzerinden gönderir.
// - Kullanıcı arayüzünden gelen AyarPaket ve KomutPaket'leri yakalama makinesi üzerinden alır.
// - AyarPaket geldiğinde ayarı kaydeder, LED'i toggle eder ve GeriBeslemePaket ile yanıtlar.
// - KomutPaket geldiğinde komutu kayıt altına alır ve GeriBeslemePaket ile yanıtlar.
// Tüm dış olaylar (UI güncellemesi için) event'ler aracılığıyla yayınlanır.
public sealed class SimulasyonMotoru : IDisposable
{
    // 5 Hz = 200 ms periyot.
    private const int PeriyotMs = 200;

    private readonly UdpHaberlesmeci _haberlesmeci;
    private readonly Stopwatch _stopwatch = new();
    private readonly Random _rastgele = new();

    // 3 thread sınırı: UDP dinleyici (1) + gönderici thread (1) = 2.
    private Thread? _gondericiThread;
    private CancellationTokenSource? _iptalKaynagi;
    private volatile bool _calisiyorMu;

    // Mevcut ayar değerleri (UI ile aynı anda okumak/yazmak için kilit altında).
    private readonly Dictionary<AyarTipi, float> _ayarDegerleri = new()
    {
        [AyarTipi.IrtifaAyari]   = 0f,
        [AyarTipi.HizAyari]      = 0f,
        [AyarTipi.SicaklikAyari] = 0f
    };

    private readonly object _ayarKilit = new();
    private bool _ledYanikMi;

    // Telemetri paketi gönderildiğinde tetiklenir (UI'a gösterim için).
    public event EventHandler<TelemetriGonderildiEventArgs>? TelemetriGonderildi;

    // Ayar paketi geldiğinde LED durumu değiştiğinde tetiklenir.
    public event EventHandler<LedDurumuDegistiEventArgs>? LedDurumuDegisti;

    // Komut paketi alındığında tetiklenir.
    public event EventHandler<KomutAlindiEventArgs>? KomutAlindi;

    // Hatalı paket yakalandığında tetiklenir (sayaç + UI bildirimi için).
    public event EventHandler<HataliPaketYakalandiEventArgs>? HataliPaketYakalandi;

    // Aktif paket yakalama makinesi (UI'da sayaç göstermek için).
    public PaketYakalamaMakinesi Yakalama => _haberlesmeci.Yakalama;

    public bool CalisiyorMu => _calisiyorMu;

    public SimulasyonMotoru(HaberlesmeAyarlari ayarlar)
    {
        if (ayarlar is null) throw new ArgumentNullException(nameof(ayarlar));

        _haberlesmeci = new UdpHaberlesmeci(ayarlar);
        _haberlesmeci.Yakalama.PaketYakalandi += YakalamaPaketYakalandi;
        _haberlesmeci.Yakalama.HataliPaketYakalandi += YakalamaHataliPaket;
    }

    // Simülasyonu başlatır: UDP dinleyici + 5 Hz gönderici thread.
    public void Baslat()
    {
        if (_calisiyorMu) return;

        _haberlesmeci.Baslat();
        _stopwatch.Restart();

        _iptalKaynagi = new CancellationTokenSource();
        _calisiyorMu = true;

        _gondericiThread = new Thread(GondericiDongusu)
        {
            IsBackground = true,
            Name = "Simulasyon-Gonderici"
        };
        _gondericiThread.Start(_iptalKaynagi.Token);
    }

    // Simülasyonu güvenli şekilde durdurur.
    public void Durdur()
    {
        if (!_calisiyorMu) return;

        _calisiyorMu = false;

        try { _iptalKaynagi?.Cancel(); } catch { /* yutulur */ }

        _gondericiThread?.Join(TimeSpan.FromSeconds(1));
        _gondericiThread = null;

        _iptalKaynagi?.Dispose();
        _iptalKaynagi = null;

        _haberlesmeci.Durdur();
        _stopwatch.Stop();
    }

    // 5 Hz periyotla iki telemetri paketini üretip gönderir.
    private void GondericiDongusu(object? parametre)
    {
        var token = (CancellationToken)parametre!;
        uint paketSayaci = 0;

        while (_calisiyorMu && !token.IsCancellationRequested)
        {
            try
            {
                paketSayaci++;
                HaberlesmePaket1Gonder(paketSayaci);
                HaberlesmePaket2Gonder();
            }
            catch
            {
                // Gönderim sırasında geçici hata olabilir; döngüyü bozmadan devam ederiz.
            }

            // 5 Hz periyot: 200 ms. Thread.Sleep tercih edildi çünkü "timer" kuralı yasak.
            try
            {
                Thread.Sleep(PeriyotMs);
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    // Örnek HaberlesmePaket1 verisi üretir ve gönderir.
    private void HaberlesmePaket1Gonder(uint sayac)
    {
        var paket = new HaberlesmePaket1
        {
            Veri1_u8 = (byte)(sayac & 0xFF),
            Veri2_u8 = (byte)((sayac >> 1) & 0xFF),
            Veri3_u8 = (byte)_rastgele.Next(0, 256),
            Veri4_s16 = (short)_rastgele.Next(-1000, 1000),
            Veri5_s16 = (short)_rastgele.Next(-500, 500),
            Veri6_f32 = (float)(Math.Sin(sayac * 0.1) * 100.0),
            Veri7_f32 = (float)(Math.Cos(sayac * 0.1) * 100.0)
        };

        byte[] paketBytes = PaketSerilestirici.Serilestir(paket);
        _haberlesmeci.Gonder(paketBytes);

        TelemetriGonderildi?.Invoke(this, new TelemetriGonderildiEventArgs(paket));
    }

    // Örnek HaberlesmePaket2 verisi üretir ve gönderir.
    private void HaberlesmePaket2Gonder()
    {
        var paket = new HaberlesmePaket2
        {
            Veri1_u8 = (byte)_rastgele.Next(0, 256),
            Veri2_u8 = (byte)_rastgele.Next(0, 256),
            Veri3_s16 = (short)_rastgele.Next(-2000, 2000),
            Veri4_s32 = _rastgele.Next(-100000, 100000),
            SistemZamani_u32 = (uint)_stopwatch.ElapsedMilliseconds,
            Veri6_f64 = _rastgele.NextDouble() * 10000.0
        };

        byte[] paketBytes = PaketSerilestirici.Serilestir(paket);
        _haberlesmeci.Gonder(paketBytes);

        TelemetriGonderildi?.Invoke(this, new TelemetriGonderildiEventArgs(paket));
    }

    // Yakalama makinesi sağlıklı bir paket çözünce çağrılır.
    private void YakalamaPaketYakalandi(object? gonderen, PaketYakalandiEventArgs e)
    {
        switch (e.Paket.PaketTipi)
        {
            case UnitePaketTipleri.AyarPaket:
                AyarPaketiIsle(e.Paket.Veri);
                break;

            case UnitePaketTipleri.KomutPaket:
                KomutPaketiIsle(e.Paket.Veri);
                break;
        }
    }

    // Hatalı paket bilgisini yukarıya yayınlar.
    private void YakalamaHataliPaket(object? gonderen, HataliPaketYakalandiEventArgs e)
    {
        HataliPaketYakalandi?.Invoke(this, e);
    }

    // AyarPaket içeriğini işler: değeri günceller, LED'i toggle eder, geri besleme döner.
    private void AyarPaketiIsle(byte[] veri)
    {
        AyarPaket ayar = PaketSerilestirici.AyarPaketCoz(veri);
        bool ledYeniDurum;

        lock (_ayarKilit)
        {
            _ayarDegerleri[ayar.Ayar] = ayar.Deger_f32;
            _ledYanikMi = !_ledYanikMi;
            ledYeniDurum = _ledYanikMi;
        }

        // UI'a LED değişikliği bildirimi.
        LedDurumuDegisti?.Invoke(this, new LedDurumuDegistiEventArgs(ledYeniDurum, ayar.Ayar, ayar.Deger_f32));

        // GeriBeslemePaket: ilgili ayar için cevap tipi seçilir.
        AyarSorguTipi cevapTipi = ayar.Ayar switch
        {
            AyarTipi.IrtifaAyari   => AyarSorguTipi.IrtifaAyarCevabi,
            AyarTipi.HizAyari      => AyarSorguTipi.HizAyarCevabi,
            AyarTipi.SicaklikAyari => AyarSorguTipi.SicaklikAyarCevabi,
            _                      => AyarSorguTipi.Yok
        };

        var geriBesleme = new GeriBeslemePaket
        {
            CevapTipi = cevapTipi,
            Deger_f32 = ayar.Deger_f32
        };

        _haberlesmeci.Gonder(PaketSerilestirici.Serilestir(geriBesleme));
    }

    // KomutPaket içeriğini işler ve KomutCevabi olarak geri besleme döner.
    private void KomutPaketiIsle(byte[] veri)
    {
        KomutPaket komut = PaketSerilestirici.KomutPaketCoz(veri);

        KomutAlindi?.Invoke(this, new KomutAlindiEventArgs(komut.Komut));

        // Geri besleme: komutun byte değeri "değer" alanında döner (denetim/log amaçlı).
        var geriBesleme = new GeriBeslemePaket
        {
            CevapTipi = AyarSorguTipi.KomutCevabi,
            Deger_f32 = (byte)komut.Komut
        };

        _haberlesmeci.Gonder(PaketSerilestirici.Serilestir(geriBesleme));
    }

    public void Dispose()
    {
        Durdur();
        _haberlesmeci.Yakalama.PaketYakalandi -= YakalamaPaketYakalandi;
        _haberlesmeci.Yakalama.HataliPaketYakalandi -= YakalamaHataliPaket;
        _haberlesmeci.Dispose();
    }
}
