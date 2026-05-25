namespace OrtakKutuphane.Test;

// JSON test scriptinde bir adımı temsil eden model.
// Adımın türüne göre ilgili parametre alanları doldurulur.
public sealed class TestAdimi
{
    // Adımın türü (JSON'da string olarak yazılır).
    public TestAdimTipi Tip { get; set; }

    // Rapor ve UI'da görünecek açıklama metni.
    public string Aciklama { get; set; } = string.Empty;

    // Adım sonrası beklenecek süre (saniye). Olayların yetişmesi için kullanılır.
    public double BeklemeSaniye { get; set; }

    // TelemetriAkisiniDogrula için minimum beklenen doğru paket sayısı.
    public long? MinimumDogruPaket { get; set; }

    // KomutGonder için gönderilecek komut tipi (KomutTipi enum'unun string adı).
    public string? Komut { get; set; }

    // AyarGonder için gönderilecek ayar tipi (AyarTipi enum'unun string adı).
    public string? Ayar { get; set; }

    // AyarGonder için değer.
    public double? Deger { get; set; }
}
