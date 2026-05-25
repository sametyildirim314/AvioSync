using KullaniciArayuzu.Haberlesme;
using KullaniciArayuzu.Modeller;
using KullaniciArayuzu.Test;
using Microsoft.Win32;
using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Haberlesme;
using OrtakKutuphane.Loglama;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KullaniciArayuzu;

// Kullanıcı arayüzünün ana penceresi (XAML kod-arkası).
// Aviyonik simülasyonu ile UDP üzerinden haberleşir, gelen paketleri tabloda gösterir,
// komut/ayar paketleri gönderir, telemetriyi JSONL olarak loglar, log oynatır ve
// MATLAB .mat dosyasına dönüşüm yapar.
public partial class MainWindow : Window
{
    // DataGrid'lerde aynı anda gösterilen maksimum satır sayısı (FIFO).
    private const int MaksimumSatirSayisi = 200;

    private readonly KullaniciIstemcisi _istemci;

    // Telemetri tablolarının kaynak koleksiyonları.
    private readonly ObservableCollection<HaberlesmePaket1Goruntu> _paket1Listesi = new();
    private readonly ObservableCollection<HaberlesmePaket2Goruntu> _paket2Listesi = new();

    // Sıra numaraları (UI thread içinde artırılır).
    private int _paket1SiraNo;
    private int _paket2SiraNo;

    // Canlı loglama / oynatma için durum.
    private TelemetriLogYazici? _logYazici;
    private TelemetriLogOynatici? _logOynatici;
    private string? _secilenLogDosyasi;

    // Çıktı klasörleri (exe yanında /Release/...).
    private string LogKlasoru =>
        Path.Combine(AppContext.BaseDirectory, "Release", TelemetriLogYazici.VarsayilanKlasorAdi);

    private string MatKlasoru =>
        Path.Combine(AppContext.BaseDirectory, "Release", MatlabDonusturucu.VarsayilanKlasorAdi);

    public MainWindow()
    {
        InitializeComponent();

        // Bağlantı ayarları: kullanıcı arayüzü 5001'i dinler, simülasyona (5002) gönderir.
        var ayarlar = HaberlesmeAyarlari.KullaniciArayuzuIcin();
        _istemci = new KullaniciIstemcisi(ayarlar);
        txtBaglantiBilgisi.Text =
            $"UDP Dinleme: {ayarlar.YerelDinlemeIp}:{ayarlar.YerelDinlemePortu} | " +
            $"Hedef: {ayarlar.HedefIp}:{ayarlar.HedefPort}";

        // ComboBox'ları enum değerleri ile doldur.
        cbKomut.ItemsSource = Enum.GetValues<KomutTipi>();
        cbKomut.SelectedIndex = 0;
        cbAyar.ItemsSource = Enum.GetValues<AyarTipi>();
        cbAyar.SelectedIndex = 0;

        // DataGrid'lere veri kaynağı ata.
        dgPaket1.ItemsSource = _paket1Listesi;
        dgPaket2.ItemsSource = _paket2Listesi;
        dgPaket1.AutoGeneratingColumn += SutunBaslikGuzellestir;
        dgPaket2.AutoGeneratingColumn += SutunBaslikGuzellestir;

        // İstemci event'lerine abone ol.
        _istemci.HaberlesmePaket1Alindi += IstemciPaket1Alindi;
        _istemci.HaberlesmePaket2Alindi += IstemciPaket2Alindi;
        _istemci.GeriBeslemeAlindi += IstemciGeriBeslemeAlindi;
        _istemci.HataliPaketYakalandi += IstemciHataliPaket;
        _istemci.Yakalama.PaketYakalandi += IstemciSayacGuncelle;

        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _logOynatici?.Durdur();
            _logYazici?.Dispose();
            _istemci.Dispose();
        }
        catch { /* yutulur */ }
    }

    // DataGrid başlıklarını sade gösterir (SiraNo -> "Sıra", Veri1_u8 -> "Veri 1 (u8)").
    private void SutunBaslikGuzellestir(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        e.Column.Header = e.PropertyName switch
        {
            "SiraNo"            => "Sıra",
            "Zaman"             => "Zaman",
            "Veri1_u8"          => "Veri 1 (u8)",
            "Veri2_u8"          => "Veri 2 (u8)",
            "Veri3_u8"          => "Veri 3 (u8)",
            "Veri3_s16"         => "Veri 3 (s16)",
            "Veri4_s16"         => "Veri 4 (s16)",
            "Veri5_s16"         => "Veri 5 (s16)",
            "Veri4_s32"         => "Veri 4 (s32)",
            "SistemZamani_u32"  => "Sistem Zamanı (u32)",
            "Veri6_f32"         => "Veri 6 (f32)",
            "Veri7_f32"         => "Veri 7 (f32)",
            "Veri6_f64"         => "Veri 6 (f64)",
            _ => e.PropertyName
        };
    }

    #region BAĞLANTI

    private void BtnBaglan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _istemci.Baslat();
            btnBaglan.IsEnabled = false;
            btnKes.IsEnabled = true;
            btnKomutGonder.IsEnabled = true;
            btnAyarGonder.IsEnabled = true;
            btnLogBaslat.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Bağlantı kurulamadı:\n{ex.Message}",
                "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnKes_Click(object sender, RoutedEventArgs e)
    {
        _istemci.Durdur();
        btnBaglan.IsEnabled = true;
        btnKes.IsEnabled = false;
        btnKomutGonder.IsEnabled = false;
        btnAyarGonder.IsEnabled = false;
        btnLogBaslat.IsEnabled = false;
        BtnLogDurdur_Click(sender, e);
    }

    // Otonom test motorunu ayrı bir pencerede açar; UI Automation ana pencereye uygulanır.
    private void BtnOtonomTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pencere = new TestPenceresi(this, _istemci)
            {
                Owner = this
            };
            pencere.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Test motoru başlatılamadı:\n{ex.Message}",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region KOMUT / AYAR

    private void BtnKomutGonder_Click(object sender, RoutedEventArgs e)
    {
        if (cbKomut.SelectedItem is not KomutTipi komut) return;

        try
        {
            _istemci.KomutGonder(komut);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Komut gönderilemedi:\n{ex.Message}",
                "Gönderim Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnAyarGonder_Click(object sender, RoutedEventArgs e)
    {
        if (cbAyar.SelectedItem is not AyarTipi ayar) return;

        if (!float.TryParse(tbAyarDeger.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float deger) &&
            !float.TryParse(tbAyarDeger.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out deger))
        {
            MessageBox.Show("Geçerli bir ondalık sayı giriniz.",
                "Geçersiz Değer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _istemci.AyarGonder(ayar, deger);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ayar gönderilemedi:\n{ex.Message}",
                "Gönderim Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region İSTEMCİ OLAYLARI (Dinleyici thread -> UI)

    private void IstemciPaket1Alindi(object? sender, HaberlesmePaket1AlindiEventArgs e)
    {
        // Log yazımı UI thread'i beklemeden hemen yapılır (thread-safe).
        _logYazici?.Yaz(e.Paket);

        Dispatcher.Invoke(() =>
        {
            _paket1SiraNo++;
            _paket1Listesi.Insert(0, HaberlesmePaket1Goruntu.Olustur(_paket1SiraNo, e.Paket));
            while (_paket1Listesi.Count > MaksimumSatirSayisi)
                _paket1Listesi.RemoveAt(_paket1Listesi.Count - 1);
        });
    }

    private void IstemciPaket2Alindi(object? sender, HaberlesmePaket2AlindiEventArgs e)
    {
        _logYazici?.Yaz(e.Paket);

        Dispatcher.Invoke(() =>
        {
            _paket2SiraNo++;
            _paket2Listesi.Insert(0, HaberlesmePaket2Goruntu.Olustur(_paket2SiraNo, e.Paket));
            while (_paket2Listesi.Count > MaksimumSatirSayisi)
                _paket2Listesi.RemoveAt(_paket2Listesi.Count - 1);
        });
    }

    private void IstemciGeriBeslemeAlindi(object? sender, GeriBeslemeAlindiEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtSonGeriBeslemeTipi.Text = e.Paket.CevapTipi.ToString();
            txtSonGeriBeslemeDeger.Text = $"Değer: {e.Paket.Deger_f32:F2}";
        });
    }

    private void IstemciHataliPaket(object? sender, HataliPaketYakalandiEventArgs e)
    {
        long hatali = _istemci.Yakalama.HataliPaketSayisi;
        Dispatcher.Invoke(() => txtHataliSayac.Text = hatali.ToString());
    }

    private void IstemciSayacGuncelle(object? sender, PaketYakalandiEventArgs e)
    {
        long dogru = _istemci.Yakalama.DogruPaketSayisi;
        Dispatcher.Invoke(() => txtDogruSayac.Text = dogru.ToString());
    }

    #endregion

    #region LOGLAMA

    private void BtnLogBaslat_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logYazici = new TelemetriLogYazici(LogKlasoru);
            txtLogDurumu.Text = "Durum: Loglama açık";
            txtLogDosyaYolu.Text = _logYazici.DosyaYolu;
            btnLogBaslat.IsEnabled = false;
            btnLogDurdur.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Log dosyası oluşturulamadı:\n{ex.Message}",
                "Loglama Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnLogDurdur_Click(object sender, RoutedEventArgs e)
    {
        if (_logYazici is null) return;

        try
        {
            _logYazici.Dispose();
            txtLogDurumu.Text = $"Durum: Kapalı | Toplam kayıt: {_logYazici.YazilanKayitSayisi}";
        }
        finally
        {
            _logYazici = null;
            btnLogBaslat.IsEnabled = _istemci.CalisiyorMu;
            btnLogDurdur.IsEnabled = false;
        }
    }

    #endregion

    #region LOG OYNATMA

    private void BtnLogYukle_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Telemetri log dosyası seç",
            Filter = "JSON Lines (*.jsonl)|*.jsonl|Tüm Dosyalar (*.*)|*.*",
            InitialDirectory = Directory.Exists(LogKlasoru) ? LogKlasoru : AppContext.BaseDirectory
        };

        if (dlg.ShowDialog(this) == true)
        {
            _secilenLogDosyasi = dlg.FileName;
            txtOynatmaDosyasi.Text = Path.GetFileName(dlg.FileName);
            btnLogOynat.IsEnabled = true;
            btnMatDonustur.IsEnabled = true;
            txtMatDurumu.Text = $"Seçilen log: {Path.GetFileName(dlg.FileName)}";
            txtOynatmaIlerleme.Text = "—";
        }
    }

    private async void BtnLogOynat_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_secilenLogDosyasi)) return;

        try
        {
            _logOynatici = TelemetriLogOynatici.DosyadanOlustur(_secilenLogDosyasi);
            _logOynatici.KayitOynatildi += OynaticiKayitOynatildi;
            _logOynatici.OynatmaBitti += OynaticiBitti;

            btnLogOynat.IsEnabled = false;
            btnLogOynatDurdur.IsEnabled = true;

            // Tabloyu temizleyip baştan oynat (kullanıcı verileri karıştırmasın).
            _paket1Listesi.Clear();
            _paket2Listesi.Clear();
            _paket1SiraNo = 0;
            _paket2SiraNo = 0;

            await _logOynatici.BaslatAsync(hizCarpani: 1.0);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Oynatma başlatılamadı:\n{ex.Message}",
                "Oynatma Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            btnLogOynat.IsEnabled = true;
            btnLogOynatDurdur.IsEnabled = false;
        }
    }

    private void BtnLogOynatDurdur_Click(object sender, RoutedEventArgs e)
    {
        _logOynatici?.Durdur();
    }

    private void OynaticiKayitOynatildi(object? sender, LogOynatildiEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            txtOynatmaIlerleme.Text = $"{e.Indeks} / {e.Toplam}";

            switch (e.Kayit.PaketTipi)
            {
                case UnitePaketTipleri.HaberlesmePaket1 when e.Kayit.Paket1.HasValue:
                    _paket1SiraNo++;
                    _paket1Listesi.Insert(0, HaberlesmePaket1Goruntu.Olustur(_paket1SiraNo, e.Kayit.Paket1.Value));
                    while (_paket1Listesi.Count > MaksimumSatirSayisi)
                        _paket1Listesi.RemoveAt(_paket1Listesi.Count - 1);
                    break;

                case UnitePaketTipleri.HaberlesmePaket2 when e.Kayit.Paket2.HasValue:
                    _paket2SiraNo++;
                    _paket2Listesi.Insert(0, HaberlesmePaket2Goruntu.Olustur(_paket2SiraNo, e.Kayit.Paket2.Value));
                    while (_paket2Listesi.Count > MaksimumSatirSayisi)
                        _paket2Listesi.RemoveAt(_paket2Listesi.Count - 1);
                    break;
            }
        });
    }

    private void OynaticiBitti(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            btnLogOynat.IsEnabled = _secilenLogDosyasi is not null;
            btnLogOynatDurdur.IsEnabled = false;
        });
    }

    #endregion

    #region MATLAB DÖNÜŞÜMÜ

    private void BtnMatDonustur_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_secilenLogDosyasi))
        {
            MessageBox.Show("Önce 'DOSYA SEÇ' ile bir log dosyası seçmelisiniz.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            string matYol = MatlabDonusturucu.Donustur(_secilenLogDosyasi, MatKlasoru);
            txtMatDurumu.Text = $"Oluşturuldu: {Path.GetFileName(matYol)}";

            MessageBox.Show($"MATLAB dosyası başarıyla oluşturuldu:\n{matYol}",
                "MATLAB Dönüşümü", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            txtMatDurumu.Text = "Hata: " + ex.Message;
            MessageBox.Show($"Dönüşüm başarısız:\n{ex.Message}",
                "MAT Dönüşüm Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}
