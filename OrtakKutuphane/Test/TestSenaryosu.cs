namespace OrtakKutuphane.Test;

// Tüm test senaryosunu temsil eden JSON modelidir.
// "Release/Test/test_script.json" dosyasından okunur.
public sealed class TestSenaryosu
{
    // Senaryonun insan-okunabilir adı.
    public string TestAdi { get; set; } = "AvioSync Otonom Test";

    // Senaryonun amacını/özetini açıklayan metin.
    public string Aciklama { get; set; } = string.Empty;

    // Senaryoyu oluşturan adımlar (sırayla çalıştırılır).
    public List<TestAdimi> Adimlar { get; set; } = new();
}
