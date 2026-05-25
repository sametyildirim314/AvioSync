using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Haberlesme;
using OrtakKutuphane.Modeller;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SimulasyonArayuzu;

// Simülasyon arayüzünün ana penceresi (XAML kod-arkası).
// Aviyonik üniteyi simüle eden SimulasyonMotoru'nu yönetir ve UI'ı günceller.
// Tüm motor event'leri Dispatcher üzerinden UI thread'ine güvenle aktarılır.
public partial class MainWindow : Window
{
    // En fazla bu kadar olay log satırı tutulur (UI performansı için).
    private const int LogMaksimum = 300;

    private readonly SimulasyonMotoru _motor;

    // Sistem zamanı sayacı. Kural gereği yalnızca bu amaçla timer kullanılır.
    private readonly DispatcherTimer _sistemZamaniTimer;
    private readonly Stopwatch _calismaSuresi = new();

    // UI tarafındaki sayaçlar (motor sayaçları zaten thread-safe; UI'a yansıtmak için tutuyoruz).
    private long _gonderilenTelemetriSayisi;

    // LED için yanık/sönük renkler.
    private static readonly Brush LedYanikRengi  = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
    private static readonly Brush LedSonukRengi  = new SolidColorBrush(Color.FromRgb(0x3A, 0x28, 0x28));
    private static readonly Brush LedHaleYanik   = new SolidColorBrush(Color.FromArgb(0x99, 0xFD, 0xE0, 0x47));
    private static readonly Brush LedHaleSonuk   = new SolidColorBrush(Color.FromRgb(0x2A, 0x1F, 0x1F));
    private static readonly Brush DurumYesili    = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly Brush DurumKirmizisi = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

    public MainWindow()
    {
        InitializeComponent();

        // Aviyonik tarafının ayarları: simülasyon 5002'yi dinler, kullanıcı arayüzüne (5001) gönderir.
        var ayarlar = HaberlesmeAyarlari.SimulasyonArayuzuIcin();
        _motor = new SimulasyonMotoru(ayarlar);

        // Bağlantı bilgisini başlığa yansıt.
        txtBaglantiBilgisi.Text =
            $"UDP Dinleme: {ayarlar.YerelDinlemeIp}:{ayarlar.YerelDinlemePortu} | " +
            $"Hedef: {ayarlar.HedefIp}:{ayarlar.HedefPort} | 5 Hz telemetri";

        // Motor event'lerine abone ol (UI'a Dispatcher ile yansıtılır).
        _motor.TelemetriGonderildi += MotorTelemetriGonderildi;
        _motor.LedDurumuDegisti += MotorLedDurumuDegisti;
        _motor.KomutAlindi += MotorKomutAlindi;
        _motor.HataliPaketYakalandi += MotorHataliPaket;
        _motor.Yakalama.PaketYakalandi += MotorPaketYakalandi;

        // Sistem zamanı sayacı (PDF: simülasyon arayüzünde "sistem zamanı sayacı" için timer izinli).
        _sistemZamaniTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _sistemZamaniTimer.Tick += SistemZamaniTick;
        _sistemZamaniTimer.Start();

        Closing += MainWindow_Closing;

        OlayEkle("Arayüz hazır. BAŞLAT'a basarak telemetri akışını başlatabilirsiniz.");
    }

    #region UI Olayları

    private void BtnBaslat_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _motor.Baslat();
            _calismaSuresi.Restart();

            elpDurumIsigi.Fill = DurumYesili;
            txtDurum.Text = "ÇALIŞIYOR";
            btnBaslat.IsEnabled = false;
            btnDurdur.IsEnabled = true;

            OlayEkle("Simülasyon başlatıldı.");
        }
        catch (Exception ex)
        {
            OlayEkle($"[HATA] Başlatma başarısız: {ex.Message}");
            MessageBox.Show($"Simülasyon başlatılamadı:\n{ex.Message}",
                "Başlatma Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDurdur_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _motor.Durdur();
            _calismaSuresi.Stop();

            elpDurumIsigi.Fill = DurumKirmizisi;
            txtDurum.Text = "DURDURULDU";
            btnBaslat.IsEnabled = true;
            btnDurdur.IsEnabled = false;

            OlayEkle("Simülasyon durduruldu.");
        }
        catch (Exception ex)
        {
            OlayEkle($"[HATA] Durdurma sırasında: {ex.Message}");
        }
    }

    private void SistemZamaniTick(object? sender, EventArgs e)
    {
        // Çalışma süresi sıfırsa "00:00:00" gösterilir.
        TimeSpan sure = _calismaSuresi.Elapsed;
        txtSistemZamani.Text = $"{(int)sure.TotalHours:D2}:{sure.Minutes:D2}:{sure.Seconds:D2}";
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _sistemZamaniTimer.Stop();
        _motor.Dispose();
    }

    #endregion

    #region Motor Olayları (Dispatcher ile UI thread'ine geçiş)

    private void MotorTelemetriGonderildi(object? sender, TelemetriGonderildiEventArgs e)
    {
        long yeniSayi = Interlocked.Increment(ref _gonderilenTelemetriSayisi);

        Dispatcher.Invoke(() =>
        {
            txtGonderilenSayisi.Text = $"Gönderilen telemetri: {yeniSayi}";
        });
    }

    private void MotorLedDurumuDegisti(object? sender, LedDurumuDegistiEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            elpLed.Fill = e.YanikMi ? LedYanikRengi : LedSonukRengi;
            elpLedHale.Fill = e.YanikMi ? LedHaleYanik : LedHaleSonuk;
            txtLedDurumu.Text = e.YanikMi ? "YANIK" : "SÖNDÜ";
            txtSonAyar.Text = $"Son ayar: {e.TetikleyenAyar} = {e.YeniDeger:F2}";

            // İlgili ayar değerini panele yansıt.
            switch (e.TetikleyenAyar)
            {
                case AyarTipi.IrtifaAyari:   txtIrtifa.Text   = e.YeniDeger.ToString("F2"); break;
                case AyarTipi.HizAyari:      txtHiz.Text      = e.YeniDeger.ToString("F2"); break;
                case AyarTipi.SicaklikAyari: txtSicaklik.Text = e.YeniDeger.ToString("F2"); break;
            }

            OlayEkle($"[AYAR] {e.TetikleyenAyar} = {e.YeniDeger:F2}  |  LED: {(e.YanikMi ? "YANIK" : "SÖNDÜ")}");
        });
    }

    private void MotorKomutAlindi(object? sender, KomutAlindiEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtSonKomut.Text = e.Komut.ToString();
            OlayEkle($"[KOMUT] {e.Komut} alındı, geri besleme gönderildi.");
        });
    }

    private void MotorHataliPaket(object? sender, HataliPaketYakalandiEventArgs e)
    {
        // Sayacı UI'a yansıt + log.
        long hatali = _motor.Yakalama.HataliPaketSayisi;

        Dispatcher.Invoke(() =>
        {
            txtHataliSayac.Text = hatali.ToString();
            OlayEkle($"[HATALI PAKET] {e.Sebep}: {e.Aciklama}");
        });
    }

    private void MotorPaketYakalandi(object? sender, PaketYakalandiEventArgs e)
    {
        // Sayacı UI'a yansıt (komut/ayar paketleri çözüldükten sonra zaten ilgili event'ler ayrıca atılıyor).
        long dogru = _motor.Yakalama.DogruPaketSayisi;

        Dispatcher.Invoke(() =>
        {
            txtDogruSayac.Text = dogru.ToString();
        });
    }

    #endregion

    #region Log Yardımcısı

    // Olay günlüğüne tarih damgalı satır ekler. UI thread'inde çalıştığı varsayılır.
    private void OlayEkle(string mesaj)
    {
        string satir = $"{DateTime.Now:HH:mm:ss.fff}  {mesaj}";
        lstOlaylar.Items.Insert(0, satir);

        // En çok LogMaksimum kadar tut.
        while (lstOlaylar.Items.Count > LogMaksimum)
        {
            lstOlaylar.Items.RemoveAt(lstOlaylar.Items.Count - 1);
        }
    }

    #endregion
}
