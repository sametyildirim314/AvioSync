namespace OrtakKutuphane.Enumlar;

// Geri besleme paketinde "cevap tipi" alanını dolduran enum.
// Hangi ayara veya komuta cevap verildiğini ifade eder.
public enum AyarSorguTipi : byte
{
    // Tanımsız / boş cevap.
    Yok = 0x00,

    // İrtifa ayarı güncellendi.
    IrtifaAyarCevabi = 0x20,

    // Hız ayarı güncellendi.
    HizAyarCevabi = 0x21,

    // Sıcaklık ayarı güncellendi.
    SicaklikAyarCevabi = 0x22,

    // Komut başarıyla işlendi.
    KomutCevabi = 0x23
}
