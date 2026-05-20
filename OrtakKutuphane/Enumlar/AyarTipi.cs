namespace OrtakKutuphane.Enumlar;

// Ayar paketinde "ayar_u8" alanını dolduran enum.
// Hangi parametre değerinin değiştirilmek istendiğini ifade eder.
public enum AyarTipi : byte
{
    // İrtifa parametresini değiştirme isteği.
    IrtifaAyari = 0x20,

    // Hız parametresini değiştirme isteği.
    HizAyari = 0x21,

    // Sıcaklık parametresini değiştirme isteği.
    SicaklikAyari = 0x22
}
