namespace OrtakKutuphane.Haberlesme;

// Hatalı paket sebebini ifade eder. Loglama ve hata analizi için kullanılır.
public enum HataliPaketSebebi : byte
{
    // Henüz hata yok.
    Yok = 0,

    // İlk senkron byte'ı (169) yakalanırken araya başka byte'lar girdi.
    Senkron1Eslemedi = 1,

    // İkinci senkron byte'ı (233) yakalanmadı, ilk senkron sonrası beklenmedik byte geldi.
    Senkron2Eslemedi = 2,

    // PaketID alanı tanımlı bir UnitePaketTipleri değerine karşılık gelmedi.
    BilinmeyenPaketId = 3,

    // PaketBoyutu alanı protokol limitlerini aştı veya bilinen paket için boyut uyuşmadı.
    PaketBoyutuHatali = 4,

    // CRC-8 doğrulaması başarısız oldu (paket bozulmuş).
    CrcDogrulamasiBasarisiz = 5
}
