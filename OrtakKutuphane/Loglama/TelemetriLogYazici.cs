using OrtakKutuphane.Modeller;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OrtakKutuphane.Loglama;

// Telemetri paketlerini "JSON Lines" formatında (her satır bir JSON kaydı) dosyaya yazar.
// Thread-safe yazım için tek bir kilit kullanılır.
// "Release/Log Kayıtları" klasörü otomatik oluşturulur.
public sealed class TelemetriLogYazici : IDisposable
{
    public const string VarsayilanKlasorAdi = "Log Kayıtları";

    private readonly StreamWriter _yazici;
    private readonly object _kilit = new();
    private bool _kapatildi;

    // Yazımı yapılan log dosyasının tam yolu.
    public string DosyaYolu { get; }

    // Şu ana kadar yazılan kayıt sayısı.
    public long YazilanKayitSayisi { get; private set; }

    public TelemetriLogYazici(string klasorYolu)
    {
        if (string.IsNullOrWhiteSpace(klasorYolu))
            throw new ArgumentException("Klasör yolu boş olamaz.", nameof(klasorYolu));

        Directory.CreateDirectory(klasorYolu);

        string dosyaAdi = $"Telemetri_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl";
        DosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

        _yazici = new StreamWriter(DosyaYolu, append: false, new UTF8Encoding(false))
        {
            AutoFlush = false
        };

        // Açıklayıcı bir başlık yorum satırı yazmak yerine direkt JSONL'e başlıyoruz.
        // (JSONL standardı yorum satırı tanımlamaz.)
    }

    // HaberlesmePaket1 kaydını dosyaya yazar.
    public void Yaz(HaberlesmePaket1 paket)
    {
        var kayit = LogKaydi.Olustur(paket);
        KayitYaz(kayit);
    }

    // HaberlesmePaket2 kaydını dosyaya yazar.
    public void Yaz(HaberlesmePaket2 paket)
    {
        var kayit = LogKaydi.Olustur(paket);
        KayitYaz(kayit);
    }

    // Tek bir LogKaydı'nı serileştirip dosyaya yazar (kilit altında).
    private void KayitYaz(LogKaydi kayit)
    {
        if (_kapatildi) return;

        string satir = JsonSerializer.Serialize(kayit, LogJsonAyarlari.Varsayilan);

        lock (_kilit)
        {
            if (_kapatildi) return;
            _yazici.WriteLine(satir);
            YazilanKayitSayisi++;
        }
    }

    // Tampondaki verileri diske aktarır.
    public void Bosalt()
    {
        lock (_kilit)
        {
            if (_kapatildi) return;
            _yazici.Flush();
        }
    }

    public void Dispose()
    {
        lock (_kilit)
        {
            if (_kapatildi) return;
            _kapatildi = true;
            try { _yazici.Flush(); } catch { /* yutulur */ }
            _yazici.Dispose();
        }
    }
}
