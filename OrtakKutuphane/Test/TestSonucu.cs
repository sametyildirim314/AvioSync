namespace OrtakKutuphane.Test;

// Bir adımın yürütme sonucunu temsil eder.
public sealed class TestSonucu
{
    // Sıra numarası (1 tabanlı).
    public int SiraNo { get; set; }

    // Çalıştırılan adımın özeti (Tip + açıklama).
    public string AdimOzeti { get; set; } = string.Empty;

    // Adım başarılı mı?
    public bool Basarili { get; set; }

    // Detay mesajı (başarısızlık nedeni veya başarı detayı).
    public string Mesaj { get; set; } = string.Empty;

    // Adımın ne kadar sürdüğü.
    public TimeSpan Sure { get; set; }

    // Adımın tamamlandığı an (UTC).
    public DateTime BitisZamani { get; set; }
}
