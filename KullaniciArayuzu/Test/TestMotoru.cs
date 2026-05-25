using KullaniciArayuzu.Haberlesme;
using OrtakKutuphane.Enumlar;
using OrtakKutuphane.Modeller;
using OrtakKutuphane.Test;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace KullaniciArayuzu.Test;

// JSON test senaryosunu adım adım yürüten test motoru.
// UIAutomationYardimcisi üzerinden kullanıcı arayüzündeki butonları AutomationId ile
// tetikler, KullaniciIstemcisi'nden gelen geri besleme paketlerini izler ve her adımın
// başarılı/başarısız olduğunu raporlar.
//
// Threading: Senaryonun tamamı bir Task üzerinde (ThreadPool worker) çalışır;
// UI Automation çağrıları kendi içlerinde UI thread'ine yönlendiği için yeni Thread
// oluşturulmaz ve 3-thread limiti korunur.
public sealed class TestMotoru
{
    private readonly UIAutomationYardimcisi _ui;
    private readonly KullaniciIstemcisi _istemci;

    // Beklenen geri besleme paketini gözlemlemek için son alınan kayıt tutulur.
    private GeriBeslemePaket? _sonGeriBesleme;
    private readonly object _gbKilit = new();

    // Adım tamamlandığında UI'a bildirim yayar (canlı liste için).
    public event EventHandler<TestSonucu>? AdimTamamlandi;

    public TestMotoru(UIAutomationYardimcisi ui, KullaniciIstemcisi istemci)
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _istemci = istemci ?? throw new ArgumentNullException(nameof(istemci));
        _istemci.GeriBeslemeAlindi += GeriBeslemeAlindi;
    }

    private void GeriBeslemeAlindi(object? sender, GeriBeslemeAlindiEventArgs e)
    {
        lock (_gbKilit)
        {
            _sonGeriBesleme = e.Paket;
        }
    }

    // Senaryoyu çalıştırır. Tüm IO/UI işlemleri ThreadPool worker üzerinde döner.
    public Task<TestRaporu> CalistirAsync(TestSenaryosu senaryo, CancellationToken iptal = default)
    {
        if (senaryo is null) throw new ArgumentNullException(nameof(senaryo));

        return Task.Run(() =>
        {
            var rapor = new TestRaporu
            {
                TestAdi = senaryo.TestAdi,
                Aciklama = senaryo.Aciklama,
                BaslangicZamani = DateTime.UtcNow
            };

            for (int i = 0; i < senaryo.Adimlar.Count; i++)
            {
                iptal.ThrowIfCancellationRequested();
                var adim = senaryo.Adimlar[i];
                var sonuc = AdimCalistir(i + 1, adim, iptal);
                rapor.Sonuclar.Add(sonuc);
                AdimTamamlandi?.Invoke(this, sonuc);
            }

            rapor.BitisZamani = DateTime.UtcNow;
            return rapor;
        }, iptal);
    }

    // Tek bir adımı çalıştırır, başarı/başarısızlık ve süreyi raporlar.
    private TestSonucu AdimCalistir(int sira, TestAdimi adim, CancellationToken iptal)
    {
        var sonuc = new TestSonucu
        {
            SiraNo = sira,
            AdimOzeti = $"[{adim.Tip}] {adim.Aciklama}"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            AdimIsle(adim, iptal);
            sonuc.Basarili = true;
            sonuc.Mesaj = "Başarılı";
        }
        catch (OperationCanceledException)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = "Test iptal edildi.";
            throw;
        }
        catch (Exception ex)
        {
            sonuc.Basarili = false;
            sonuc.Mesaj = ex.Message;
        }
        finally
        {
            sw.Stop();
            sonuc.Sure = sw.Elapsed;
            sonuc.BitisZamani = DateTime.UtcNow;
        }

        return sonuc;
    }

    // Adım tipine göre uygun UI etkileşimi ve doğrulamayı uygular.
    private void AdimIsle(TestAdimi adim, CancellationToken iptal)
    {
        switch (adim.Tip)
        {
            case TestAdimTipi.BaglantiyiBaslat:
                _ui.Tikla("btnBaglan");
                BeklemeUygula(adim);
                break;

            case TestAdimTipi.BaglantiyiKes:
                _ui.Tikla("btnKes");
                BeklemeUygula(adim);
                break;

            case TestAdimTipi.Bekle:
                BeklemeUygula(adim, varsayilanSn: 1.0);
                break;

            case TestAdimTipi.TelemetriAkisiniDogrula:
                {
                    long baslangic = _istemci.Yakalama.DogruPaketSayisi;
                    BeklemeUygula(adim, varsayilanSn: 5.0);
                    long bitis = _istemci.Yakalama.DogruPaketSayisi;
                    long fark = bitis - baslangic;
                    long beklenen = adim.MinimumDogruPaket ?? 1;
                    if (fark < beklenen)
                    {
                        throw new InvalidOperationException(
                            $"Yetersiz telemetri akışı: beklenen >= {beklenen}, alınan {fark} (toplam {bitis}).");
                    }
                    break;
                }

            case TestAdimTipi.HataliPaketYok:
                {
                    long hatali = _istemci.Yakalama.HataliPaketSayisi;
                    if (hatali > 0)
                        throw new InvalidOperationException($"Hatalı paket sayacı 0 değil: {hatali}.");
                    break;
                }

            case TestAdimTipi.KomutGonder:
                {
                    if (string.IsNullOrWhiteSpace(adim.Komut))
                        throw new InvalidOperationException("Komut adı belirtilmemiş.");
                    if (!Enum.TryParse<KomutTipi>(adim.Komut, out _))
                        throw new InvalidOperationException($"Geçersiz KomutTipi: {adim.Komut}");

                    GeriBeslemeSifirla();
                    _ui.ComboOgesiSec("cbKomut", adim.Komut);
                    _ui.Tikla("btnKomutGonder");

                    var timeout = TimeSpan.FromSeconds(Math.Max(1.0, adim.BeklemeSaniye));
                    BeklenenGeriBeslemeBekle(AyarSorguTipi.KomutCevabi, timeout, iptal);
                    break;
                }

            case TestAdimTipi.AyarGonder:
                {
                    if (string.IsNullOrWhiteSpace(adim.Ayar))
                        throw new InvalidOperationException("Ayar adı belirtilmemiş.");
                    if (!Enum.TryParse<AyarTipi>(adim.Ayar, out var ayarTipi))
                        throw new InvalidOperationException($"Geçersiz AyarTipi: {adim.Ayar}");

                    GeriBeslemeSifirla();
                    _ui.ComboOgesiSec("cbAyar", adim.Ayar);
                    _ui.DegerYaz("tbAyarDeger",
                        (adim.Deger ?? 0).ToString("R", CultureInfo.InvariantCulture));
                    _ui.Tikla("btnAyarGonder");

                    var beklenen = ayarTipi switch
                    {
                        AyarTipi.IrtifaAyari   => AyarSorguTipi.IrtifaAyarCevabi,
                        AyarTipi.HizAyari      => AyarSorguTipi.HizAyarCevabi,
                        AyarTipi.SicaklikAyari => AyarSorguTipi.SicaklikAyarCevabi,
                        _                      => AyarSorguTipi.Yok
                    };

                    var timeout = TimeSpan.FromSeconds(Math.Max(1.0, adim.BeklemeSaniye));
                    BeklenenGeriBeslemeBekle(beklenen, timeout, iptal);
                    break;
                }

            default:
                throw new InvalidOperationException($"Bilinmeyen adım tipi: {adim.Tip}");
        }
    }

    // Adımda belirtilen bekleme süresini uygular (varsayılan değer destekli).
    private static void BeklemeUygula(TestAdimi adim, double varsayilanSn = 0)
    {
        double saniye = adim.BeklemeSaniye > 0 ? adim.BeklemeSaniye : varsayilanSn;
        if (saniye <= 0) return;
        Thread.Sleep(TimeSpan.FromSeconds(saniye));
    }

    // Belirtilen tip ve süre içinde geri besleme paketinin gelmesini bekler.
    private void BeklenenGeriBeslemeBekle(AyarSorguTipi beklenen, TimeSpan timeout, CancellationToken iptal)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            iptal.ThrowIfCancellationRequested();

            GeriBeslemePaket? son;
            lock (_gbKilit) son = _sonGeriBesleme;

            if (son.HasValue && son.Value.CevapTipi == beklenen)
                return;

            Thread.Sleep(50);
        }

        string mevcut;
        lock (_gbKilit)
        {
            mevcut = _sonGeriBesleme.HasValue
                ? $"{_sonGeriBesleme.Value.CevapTipi} = {_sonGeriBesleme.Value.Deger_f32:F2}"
                : "(hiç geri besleme gelmedi)";
        }

        throw new TimeoutException(
            $"Beklenen geri besleme '{beklenen}' {timeout.TotalSeconds:F1}s içinde gelmedi. Son alınan: {mevcut}");
    }

    private void GeriBeslemeSifirla()
    {
        lock (_gbKilit)
        {
            _sonGeriBesleme = null;
        }
    }
}
