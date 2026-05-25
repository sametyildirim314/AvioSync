using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrtakKutuphane.Test;

// Test senaryosunu JSON dosyasından yükleme/yazma için merkezi yardımcı.
// Enumlar string olarak yazılır (insan-okunabilir).
public static class TestSenaryoYukleyici
{
    // PDF gereği klasör adı "Test" olarak sabittir.
    public const string VarsayilanKlasorAdi = "Test";

    // Varsayılan dosya adı.
    public const string VarsayilanDosyaAdi = "test_script.json";

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    // JSON dosyasından senaryo okur.
    public static TestSenaryosu Yukle(string dosyaYolu)
    {
        if (!File.Exists(dosyaYolu))
            throw new FileNotFoundException("Test script dosyası bulunamadı.", dosyaYolu);

        string icerik = File.ReadAllText(dosyaYolu);
        var senaryo = JsonSerializer.Deserialize<TestSenaryosu>(icerik, _json)
            ?? throw new InvalidDataException("Test script JSON boş veya geçersiz.");

        return senaryo;
    }

    // Senaryoyu JSON olarak yazar (PDF'in beklediği "varsayılan script" üretimi için).
    public static void Yaz(string dosyaYolu, TestSenaryosu senaryo)
    {
        string? klasor = Path.GetDirectoryName(dosyaYolu);
        if (!string.IsNullOrEmpty(klasor))
            Directory.CreateDirectory(klasor);

        string icerik = JsonSerializer.Serialize(senaryo, _json);
        File.WriteAllText(dosyaYolu, icerik);
    }
}
