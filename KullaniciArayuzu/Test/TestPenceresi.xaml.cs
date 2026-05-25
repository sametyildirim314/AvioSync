using KullaniciArayuzu.Haberlesme;
using Microsoft.Win32;
using OrtakKutuphane.Test;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace KullaniciArayuzu.Test;

// Otonom test motorunu yöneten ayrı pencere.
// Kullanıcı arayüzünün ana penceresi açıkken bu pencere üzerinden bir test_script.json
// yüklenip "TESTİ ÇALIŞTIR" butonuna basıldığında, UI Automation API ile ana arayüzdeki
// butonlar tetiklenir; her adımın sonucu canlı tabloda görünür ve test bittiğinde
// otomatik olarak PDF raporu üretilir.
public partial class TestPenceresi : Window
{
    // DataGrid satırı için görüntü modeli (TestSonucu -> okunabilir alanlar).
    public sealed class AdimSonucGoruntu
    {
        public int SiraNo { get; set; }
        public string DurumYazisi { get; set; } = "";
        public string AdimOzeti { get; set; } = "";
        public string SureSn { get; set; } = "";
        public string Mesaj { get; set; } = "";
    }

    private readonly Window _hedefPencere;
    private readonly KullaniciIstemcisi _istemci;
    private readonly ObservableCollection<AdimSonucGoruntu> _adimlar = new();

    private TestSenaryosu? _yuklenenSenaryo;
    private string? _yuklenenScriptYolu;
    private CancellationTokenSource? _iptal;
    private string? _sonPdfYolu;

    // PDF rapor klasörü exe yanında /Release/Test altında oluşur (PDF'in istediği konum).
    private string TestKlasoru =>
        Path.Combine(AppContext.BaseDirectory, "Release", TestPdfRaporlayici.VarsayilanKlasorAdi);

    // Test scripti aranacak varsayılan dosya yolu.
    private string VarsayilanScriptYolu =>
        Path.Combine(TestKlasoru, TestSenaryoYukleyici.VarsayilanDosyaAdi);

    public TestPenceresi(Window hedefPencere, KullaniciIstemcisi istemci)
    {
        InitializeComponent();

        _hedefPencere = hedefPencere ?? throw new ArgumentNullException(nameof(hedefPencere));
        _istemci = istemci ?? throw new ArgumentNullException(nameof(istemci));

        dgSonuclar.ItemsSource = _adimlar;

        // Otomatik yükleme: varsayılan script varsa ön-yükleyelim.
        if (File.Exists(VarsayilanScriptYolu))
        {
            ScriptiYukle(VarsayilanScriptYolu);
        }
    }

    #region BUTON OLAYLARI

    private void BtnKapat_Click(object sender, RoutedEventArgs e)
    {
        // Test çalışıyorsa kullanıcıya sor.
        if (_iptal is not null && !_iptal.IsCancellationRequested)
        {
            var cevap = MessageBox.Show("Test devam ediyor. İptal edip pencereyi kapatmak istiyor musunuz?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (cevap != MessageBoxResult.Yes) return;
            _iptal.Cancel();
        }

        Close();
    }

    private void BtnScriptYukle_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Test scripti seç",
            Filter = "JSON Dosyaları (*.json)|*.json|Tüm Dosyalar (*.*)|*.*",
            InitialDirectory = Directory.Exists(TestKlasoru) ? TestKlasoru : AppContext.BaseDirectory
        };

        if (dlg.ShowDialog(this) == true)
        {
            ScriptiYukle(dlg.FileName);
        }
    }

    private async void BtnTestCalistir_Click(object sender, RoutedEventArgs e)
    {
        if (_yuklenenSenaryo is null)
        {
            MessageBox.Show("Önce bir test scripti yükleyin.", "Bilgi",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _adimlar.Clear();
        _sonPdfYolu = null;
        btnPdfAc.IsEnabled = false;
        txtPdfYolu.Text = "";
        txtOzet.Text = "Test çalışıyor...";

        btnTestCalistir.IsEnabled = false;
        btnScriptYukle.IsEnabled = false;
        btnTestDurdur.IsEnabled = true;

        _iptal = new CancellationTokenSource();

        try
        {
            // UI Automation, ana pencerenin HWND'sine ihtiyaç duyar; ana pencere zaten yüklü olmalı.
            var ui = new UIAutomationYardimcisi(_hedefPencere);
            var motor = new TestMotoru(ui, _istemci);
            motor.AdimTamamlandi += MotorAdimTamamlandi;

            TestRaporu rapor = await motor.CalistirAsync(_yuklenenSenaryo, _iptal.Token);

            // PDF üret.
            string pdfYolu = TestPdfRaporlayici.PdfUret(rapor, TestKlasoru);
            _sonPdfYolu = pdfYolu;

            // Özet yansıt.
            txtOzet.Text = (rapor.NihaiSonuc ? "✔ NİHAİ SONUÇ: BAŞARILI  |  " : "✘ NİHAİ SONUÇ: BAŞARISIZ  |  ") +
                $"Toplam {rapor.ToplamAdim} adım, {rapor.BasariliAdimSayisi} başarılı, {rapor.BasarisizAdimSayisi} başarısız" +
                $"  |  Süre: {rapor.ToplamSure.TotalSeconds:F2} sn";
            txtPdfYolu.Text = "PDF raporu: " + pdfYolu;
            btnPdfAc.IsEnabled = true;
        }
        catch (OperationCanceledException)
        {
            txtOzet.Text = "Test kullanıcı tarafından iptal edildi.";
        }
        catch (Exception ex)
        {
            txtOzet.Text = $"Hata: {ex.Message}";
            MessageBox.Show($"Test sırasında beklenmeyen hata:\n{ex.Message}",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _iptal?.Dispose();
            _iptal = null;
            btnTestCalistir.IsEnabled = _yuklenenSenaryo is not null;
            btnScriptYukle.IsEnabled = true;
            btnTestDurdur.IsEnabled = false;
        }
    }

    private void BtnTestDurdur_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _iptal?.Cancel();
        }
        catch { /* yutulur */ }
    }

    private void BtnPdfAc_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_sonPdfYolu) || !File.Exists(_sonPdfYolu))
        {
            MessageBox.Show("PDF dosyası bulunamadı.", "Bilgi",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_sonPdfYolu) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF açılamadı:\n{ex.Message}",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    // Test motoru adımı bitirdikçe canlı olarak DataGrid'e yansıtır.
    private void MotorAdimTamamlandi(object? sender, TestSonucu e)
    {
        Dispatcher.Invoke(() =>
        {
            _adimlar.Add(new AdimSonucGoruntu
            {
                SiraNo = e.SiraNo,
                DurumYazisi = e.Basarili ? "BAŞARILI" : "BAŞARISIZ",
                AdimOzeti = e.AdimOzeti,
                SureSn = e.Sure.TotalSeconds.ToString("F2"),
                Mesaj = e.Mesaj
            });
        });
    }

    // Bir script dosyasını UI'a yansıtır.
    private void ScriptiYukle(string dosyaYolu)
    {
        try
        {
            var senaryo = TestSenaryoYukleyici.Yukle(dosyaYolu);
            _yuklenenSenaryo = senaryo;
            _yuklenenScriptYolu = dosyaYolu;

            txtScriptYolu.Text = dosyaYolu;
            txtScriptOzet.Text = $"\"{senaryo.TestAdi}\" — {senaryo.Adimlar.Count} adım";
            btnTestCalistir.IsEnabled = true;

            _adimlar.Clear();
            txtOzet.Text = "Yüklendi. Çalıştırmaya hazır.";
            txtPdfYolu.Text = "";
            btnPdfAc.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Script yüklenemedi:\n{ex.Message}",
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
