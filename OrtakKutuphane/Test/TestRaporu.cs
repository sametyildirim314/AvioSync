namespace OrtakKutuphane.Test;

// Tüm test koşumunun (senaryo + tüm adım sonuçları) toplu raporu.
// PDF rapor üretiminde girdi olarak kullanılır.
public sealed class TestRaporu
{
    public string TestAdi { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public DateTime BaslangicZamani { get; set; }
    public DateTime BitisZamani { get; set; }
    public List<TestSonucu> Sonuclar { get; set; } = new();

    public int ToplamAdim => Sonuclar.Count;
    public int BasariliAdimSayisi => Sonuclar.Count(s => s.Basarili);
    public int BasarisizAdimSayisi => Sonuclar.Count(s => !s.Basarili);
    public bool NihaiSonuc => BasarisizAdimSayisi == 0 && Sonuclar.Count > 0;
    public TimeSpan ToplamSure => BitisZamani - BaslangicZamani;
}
