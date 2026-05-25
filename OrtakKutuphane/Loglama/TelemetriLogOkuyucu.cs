using System.IO;
using System.Text.Json;

namespace OrtakKutuphane.Loglama;

// Daha önce yazılmış bir JSONL log dosyasını satır satır okuyup LogKaydı dizisi üretir.
public static class TelemetriLogOkuyucu
{
    // Belirtilen log dosyasının tamamını belleğe alarak okur.
    // Büyük dosyalarda da kullanılabilir (her satır bağımsız parse edilir).
    public static IReadOnlyList<LogKaydi> TumKayitlariOku(string dosyaYolu)
    {
        if (!File.Exists(dosyaYolu))
            throw new FileNotFoundException("Log dosyası bulunamadı.", dosyaYolu);

        var sonuc = new List<LogKaydi>();

        // Birden fazla uygulama aynı anda dosyaya erişebilir; paylaşımlı okuma açılır.
        using var fs = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var okuyucu = new StreamReader(fs);

        string? satir;
        int satirNo = 0;
        while ((satir = okuyucu.ReadLine()) is not null)
        {
            satirNo++;
            if (string.IsNullOrWhiteSpace(satir)) continue;

            try
            {
                var kayit = JsonSerializer.Deserialize<LogKaydi>(satir, LogJsonAyarlari.Varsayilan);
                if (kayit is not null)
                    sonuc.Add(kayit);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(dosyaYolu)} - Satır {satirNo}: JSON parse hatası.", ex);
            }
        }

        return sonuc;
    }
}
