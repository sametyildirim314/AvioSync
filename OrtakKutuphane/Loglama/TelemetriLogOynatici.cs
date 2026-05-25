using System.Threading;

namespace OrtakKutuphane.Loglama;

// Bir log dosyasını orijinal zaman aralıklarına sadık kalarak (veya hız çarpanı ile)
// async olarak oynatan sınıf. ThreadPool/Task.Delay üzerinden çalıştığı için projedeki
// 3 thread sınırını etkilemez (kendi thread'i yoktur).
public sealed class TelemetriLogOynatici
{
    private readonly IReadOnlyList<LogKaydi> _kayitlar;
    private CancellationTokenSource? _iptal;

    // Tek paketi oynatma anında tetiklenir.
    public event EventHandler<LogOynatildiEventArgs>? KayitOynatildi;

    // Oynatma sona erdiğinde (normal bitiş ya da iptal) tetiklenir.
    public event EventHandler? OynatmaBitti;

    // Çalıyor mu?
    public bool CaliyorMu => _iptal is not null && !_iptal.IsCancellationRequested;

    // Toplam kayıt sayısı.
    public int ToplamKayitSayisi => _kayitlar.Count;

    public TelemetriLogOynatici(IReadOnlyList<LogKaydi> kayitlar)
    {
        _kayitlar = kayitlar ?? throw new ArgumentNullException(nameof(kayitlar));
    }

    // Hazır bir log dosyasından oynatıcı üretir.
    public static TelemetriLogOynatici DosyadanOlustur(string dosyaYolu)
    {
        var kayitlar = TelemetriLogOkuyucu.TumKayitlariOku(dosyaYolu);
        return new TelemetriLogOynatici(kayitlar);
    }

    // Oynatmayı başlatır. Önceki çalışan oynatma iptal edilir.
    // hizCarpani = 1.0 -> orijinal hız, 2.0 -> 2 kat hızlı, 0.5 -> yarı hız.
    public Task BaslatAsync(double hizCarpani = 1.0)
    {
        Durdur();

        var iptalKaynagi = new CancellationTokenSource();
        _iptal = iptalKaynagi;

        return Task.Run(() => OynatmaDongusu(hizCarpani, iptalKaynagi.Token), iptalKaynagi.Token);
    }

    // Oynatmayı durdurur.
    public void Durdur()
    {
        if (_iptal is not null)
        {
            try { _iptal.Cancel(); } catch { /* yutulur */ }
            _iptal.Dispose();
            _iptal = null;
        }
    }

    // Asıl oynatma döngüsü. Her kaydı orijinal zaman aralığına göre yayınlar.
    private async Task OynatmaDongusu(double hizCarpani, CancellationToken iptal)
    {
        if (_kayitlar.Count == 0)
        {
            OynatmaBitti?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (hizCarpani <= 0) hizCarpani = 1.0;

        DateTime baslangic = _kayitlar[0].Zaman;

        try
        {
            for (int i = 0; i < _kayitlar.Count; i++)
            {
                iptal.ThrowIfCancellationRequested();

                var kayit = _kayitlar[i];
                TimeSpan beklemeGerekli = TimeSpan.FromMilliseconds(
                    (kayit.Zaman - baslangic).TotalMilliseconds / hizCarpani);

                // Çok küçük (negatif/sıfır) gecikmeler için bekleme atlanır.
                if (i > 0 && beklemeGerekli > TimeSpan.Zero)
                {
                    TimeSpan oncekiBekleme = TimeSpan.FromMilliseconds(
                        (_kayitlar[i - 1].Zaman - baslangic).TotalMilliseconds / hizCarpani);

                    TimeSpan delta = beklemeGerekli - oncekiBekleme;
                    if (delta > TimeSpan.Zero)
                    {
                        await Task.Delay(delta, iptal).ConfigureAwait(false);
                    }
                }

                KayitOynatildi?.Invoke(this, new LogOynatildiEventArgs(kayit, i + 1, _kayitlar.Count));
            }
        }
        catch (OperationCanceledException)
        {
            // İptal beklenen bir son; sessizce çıkılır.
        }
        finally
        {
            OynatmaBitti?.Invoke(this, EventArgs.Empty);
        }
    }
}
